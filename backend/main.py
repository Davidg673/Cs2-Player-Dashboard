from datetime import  datetime

from fastapi import  FastAPI, Request, HTTPException
from fastapi.responses import  RedirectResponse
from urllib.parse import  urlencode
import  requests
from  fastapi.middleware.cors import  CORSMiddleware
from requests import RequestException

from sqlalchemy import create_engine, Table, column, Integer, String, MetaData, Select, CreateEnginePlugin, Column, \
    DateTime, PrimaryKeyConstraint,select
from sqlalchemy.exc import SQLAlchemyError,OperationalError




app = FastAPI()

origins = [ ##Whitelists addresses visited by backend
    "http://localhost:5173",
    "https://prewar-lavonne-gutsily.ngrok-free.dev"
]

app.add_middleware(  #Ensures local front end can call online backend due to CORS restrictions
    CORSMiddleware,
    allow_origins = origins,
    allow_credentials = True,
    allow_methods = ["*"],
    allow_headers = ["*"],
)

STEAM_OPENID_URL = "https://steamcommunity.com/openid/login"
RETURN_URL = "https://prewar-lavonne-gutsily.ngrok-free.dev/auth/steam/callback"
FRONTEND_URL = "http://localhost:5173/dashboard"
STEAM_API_KEY = "BA3233F31757A3833B588A637B26E9C9"

#SQL Init
engine = create_engine("mysql+pymysql://server:changeme@127.0.0.1:3306/db",
                       pool_pre_ping=True,
                       connect_args={"connect_timeout": 5,"read_timeout": 5 })
metadata = MetaData()

playerStats = Table( #Table signature from C# plugin must match exactly
    "player_stats",
    metadata,
    Column("steamid",String(18),primary_key=True,nullable=False),
    Column("kills",Integer,nullable=False,default=0),
    Column("deaths", Integer, nullable=False, default=0),
    Column("assists", Integer, nullable=False, default=0),
    Column("headshots", Integer, nullable=False, default=0),
    Column("damage_dealt", Integer, nullable=False, default=0),
    Column("damage_received", Integer, nullable=False, default=0),
    Column("bomb_plants", Integer, nullable=False, default=0),
    Column("bomb_defuses", Integer, nullable=False, default=0),
    Column("playtime", Integer, nullable=False, default=0),
    Column("last_played", DateTime, nullable=False, default=datetime.now),
    Column("rounds_won", Integer, nullable=False, default=0),
    Column("rounds_lost", Integer, nullable=False, default=0),

)
weaponStats = Table(
    "player_weapon_stats",
    metadata,
    Column("steamid", String(18), nullable=False),
    Column("weapon", String(18), nullable=False),
    Column("kills", Integer, nullable=False, default=0),
    Column("headshots", Integer, nullable=False, default=0),
    Column("shots_hit", Integer, nullable=False, default=0),
    Column("damage_dealt", Integer, nullable=False, default=0),
    PrimaryKeyConstraint("steamid","weapon")
)

@app.get("/player/{steam_id}")
def get_player_data(steam_id: str) -> dict:
    """
    :param steam_id: id of the player
    :return: dictionary with player data as dict and player weapon data as list of dicts for each weapon row
    Raiases:
        HTTPException 503: if there is a database error
        HTTPException 404: if the player cannot be found
    """

    cmdPlayer = select(playerStats).where(playerStats.c.steamid==steam_id)
    cmdWeapons = select(weaponStats).where(playerStats.c.steamid==steam_id)

    try:
        with engine.connect() as conn:
            resultPlayer = conn.execute(cmdPlayer).first()
            resultWeapons = conn.execute(cmdWeapons)
            rows = resultWeapons.fetchall()

    except OperationalError as err:
        raise HTTPException(status_code = 503, detail ="Database service is unavailable")

    except SQLAlchemyError as err:
        raise HTTPException(status_code = 503, detail =f"An unexpected server error has occurred")

    if not (resultPlayer or resultWeapons[0]):
        raise HTTPException(status_code= 404, detail = f"Player {steam_id} not found")

    player_name = get_player_name(steam_id)

    #Map the table keys to a dictionary from the result tuple
    dataPlayer = dict(zip(playerStats.columns.keys(),resultPlayer))
    dataWeapons = [
        dict(row._mapping)
        for row in rows
    ]

    #Convert datetime to ISO format for JSON compatibility
    if isinstance(dataPlayer.get("last_played"),datetime):
        dataPlayer["last_played"] = dataPlayer["last_played"].isoformat()
    if player_name:
        dataPlayer["steamid"] = player_name

    return {
        "player":dataPlayer,
        "weapons":dataWeapons
    }

@app.get("/")
def root():
    return {"status" : "FastAPI is running"}

@app.get("/auth/steam/login")   #Decorator used by fastapi to run the function below when given url is accessed
def steam_login():  #Sends request to steam for user to log in and retrieve the steam ID
    params = {
        "openid.ns" : "http://specs.openid.net/auth/2.0",
        "openid.mode" : "checkid_setup",
        "openid.return_to" : RETURN_URL,
        "openid.realm" : "https://prewar-lavonne-gutsily.ngrok-free.dev",
        "openid.identity" : "http://specs.openid.net/auth/2.0/identifier_select",
        "openid.claimed_id" : "http://specs.openid.net/auth/2.0/identifier_select",
        "force_login" : "true"
    }
    return RedirectResponse(f"{STEAM_OPENID_URL }?{urlencode(params)}") #returns url with data



@app.get("/auth/steam/callback")  #Return from steam authentication which receives data and attempts to validate info
def steam_return(request: Request):
    query = dict(request.query_params)

    query["openid.mode"] = "check_authentication"

    r = requests.post(STEAM_OPENID_URL, data= query)  #sends request back to steam for verification

    if "is_valid:true" not in r.text:
        return RedirectResponse(f"{FRONTEND_URL}?error=invalid_login")

    steam_id = query["openid.claimed_id"].split("/")[-1]

    return RedirectResponse(f"{FRONTEND_URL}?steam_id={steam_id}")  #back to front-end


def get_player_name(steam_id: str) -> str | None:
    """
    Sends a request to steam for a steam name using given id

    :param steam_id: id of the player
    :return: steam name from steam db
    """

    url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/"
    params = {
        "key":STEAM_API_KEY,
        "steamids": steam_id
    }

    try:
        response = requests.get(url,params=params, timeout=5)
        response.raise_for_status()

        data = response.json() ##convert to python dictionary
        players = data.get("response", {}).get("players",[]) #use get here to automatically raise exceptions on entry not found

        if not players:
            return None

        return players[0]["personaname"]

    except RequestException as ex:
        print(f"Steam API request failed: {ex}")
        return None

    except (KeyError, TypeError, ValueError) as ex:
        print(f"Unexpected Steam response: {ex}")
        return  None

