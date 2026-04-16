import os
from dotenv import load_dotenv
from fastapi import Request, APIRouter
from fastapi.responses import  RedirectResponse
from urllib.parse import  urlencode
import  requests

from sqlalchemy.exc import SQLAlchemyError
from app.db import engine
from sqlalchemy.dialects.mysql import insert as mysql_insert
from app.models import users


load_dotenv()

BACKEND_URL = os.getenv("BACKEND_URL")
FRONTEND_URL = os.getenv("FRONTEND_URL")

STEAM_OPENID_URL = "https://steamcommunity.com/openid/login"

## Creates list of missing env values from their keys
missing = [k for k,v in {
    "BACKEND_URL":BACKEND_URL,
    "FRONTEND_URL":FRONTEND_URL,
}.items() if not v]

if missing:
    raise RuntimeError(f"Missing env variables: {', '.join(missing)}")

router = APIRouter(tags=["auth"])

"""
    Sends request to steam for user login. retrieves steam ID
"""
@router.get("/auth/steam/login")
def steam_login():
    params = {
        "openid.ns" : "http://specs.openid.net/auth/2.0",
        "openid.mode" : "checkid_setup",
        "openid.return_to" : f"{BACKEND_URL}/auth/steam/callback",
        "openid.realm" : f"{BACKEND_URL}",
        "openid.identity" : "http://specs.openid.net/auth/2.0/identifier_select",
        "openid.claimed_id" : "http://specs.openid.net/auth/2.0/identifier_select",
        "force_login" : "true"
    }
    return RedirectResponse(f"{STEAM_OPENID_URL}?{urlencode(params)}") #returns request to steam login url with return parameters


"""
    Return from steam authentication which receives data and attempts to validate info
"""
@router.get("/auth/steam/callback")
def steam_return(request: Request):
    query = dict(request.query_params)

    query["openid.mode"] = "check_authentication"

    r = requests.post(STEAM_OPENID_URL, data= query, timeout=5)  #sends request back to steam for verification

    if "is_valid:true" not in r.text:
        return RedirectResponse(f"{FRONTEND_URL}/dashboard?error=invalid_login")

    steam_id = query["openid.claimed_id"].split("/")[-1]

    
    result = create_user(steam_id)
    
    if result and result["error"]:
        return RedirectResponse(f"{FRONTEND_URL}/dashboard?error={result['error']}")

    return RedirectResponse(f"{FRONTEND_URL}/dashboard?steam_id={steam_id}")  #back to front-end


def create_user(returned_steam_id : str):
    try:
        with engine.begin() as conn:
            stmt = mysql_insert(users).values(
                steamid = returned_steam_id,
                username = None,
                password_hash = None,
                role = "user"
            )

            stmt = stmt.prefix_with("IGNORE")
            
            conn.execute(stmt)

    except SQLAlchemyError as sql_error:
        return {"error":str(sql_error)}