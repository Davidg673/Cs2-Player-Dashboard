from datetime import  datetime

from fastapi import HTTPException, APIRouter

from sqlalchemy import select
from sqlalchemy.exc import SQLAlchemyError,OperationalError

from app.models import playerStats,weaponStats
from app.db import engine
from app.services.steam_api import get_player_name

router = APIRouter(tags=["players"])

@router.get("/player/{steam_id}")
def get_player_data(steam_id: str) -> dict:
    """
    :param steam_id: id of the player
    :return: dictionary with player data as dict and player weapon data as list of dicts for each weapon row
    Raiases:
        HTTPException 503: if there is a database error
        HTTPException 404: if the player cannot be found
    """

    cmdPlayer = select(playerStats).where(playerStats.c.steamid==steam_id)
    cmdWeapons = select(weaponStats).where(weaponStats.c.steamid==steam_id)

    try:
        with engine.connect() as conn:
            resultPlayer = conn.execute(cmdPlayer).first()
            resultWeapons = conn.execute(cmdWeapons)
            rows = resultWeapons.fetchall()

    except OperationalError as err:
        raise HTTPException(status_code = 503, detail ="Database service is unavailable")

    except SQLAlchemyError as err:
        raise HTTPException(status_code = 503, detail =f"An unexpected server error has occurred")

    if not resultPlayer:
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