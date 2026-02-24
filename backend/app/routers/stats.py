import os
from pydantic import Field, BaseModel
from datetime import datetime
from fastapi import Header
from dotenv import load_dotenv
from fastapi import HTTPException, APIRouter
from sqlalchemy.dialects.mysql import insert as mysql_insert
from app.db import engine
from app.models import playerStats, weaponStats
import logging

load_dotenv()

router = APIRouter(tags=["stats"])

INGEST_API_KEY = os.getenv("INGEST_API_KEY")  ##Read key from .env file.

logger = logging.getLogger("stats")

class WeaponPayload(BaseModel):
    weapon: str
    kills: int = 0
    headshots: int = 0
    shots_hit: int = 0
    damage_dealt: int = 0

class PlayerPayload(BaseModel):
    steamid: str
    kills: int = 0
    deaths: int = 0
    assists: int = 0
    headshots: int = 0
    damage_dealt: int = 0
    damage_received: int = 0
    bomb_plants: int = 0
    bomb_defuses: int = 0
    playtime: int = 0
    rounds_won: int = 0
    rounds_lost: int = 0
    last_played: datetime

class IngestPayload(BaseModel):
    player: PlayerPayload
    weapons: list[WeaponPayload] = Field(default_factory= list)


@router.post("/ingest/stats")
def ingest_stats(payload : IngestPayload, x_api_key: str | None = Header(default=None)):
    if not INGEST_API_KEY:
        raise HTTPException(500,"Secure not configured (missing INGEST_API_KEY)")

    if x_api_key != INGEST_API_KEY:
        raise HTTPException(401, "Invalid API Key")

    try:
        InsertIntoTable(payload)
    except Exception:
        logger.exception("DB Write Failed")
        raise HTTPException(500,"Database Error")

    return {"ok" : True}


def InsertIntoTable(payload : IngestPayload):
    p = payload.player

    with engine.begin() as conn:
        ##Create Insert object
        stmt = mysql_insert(playerStats).values(
            steamid=p.steamid,
            kills=p.kills,
            deaths=p.deaths,
            assists=p.assists,
            headshots=p.headshots,
            damage_dealt=p.damage_dealt,
            damage_received=p.damage_received,
            bomb_plants=p.bomb_plants,
            bomb_defuses=p.bomb_defuses,
            playtime=p.playtime,
            last_played=p.last_played,
            rounds_won=p.rounds_won,
            rounds_lost=p.rounds_lost
        )
        ##Update previous object with UPDATE SQL logic
        stmt = stmt.on_duplicate_key_update(
            steamid=stmt.inserted.steamid,
            kills=playerStats.c.kills + stmt.inserted.kills,
            deaths=playerStats.c.deaths + stmt.inserted.deaths,
            assists=playerStats.c.assists + stmt.inserted.assists,
            headshots=playerStats.c.headshots + stmt.inserted.headshots,
            damage_dealt=playerStats.c.damage_dealt + stmt.inserted.damage_dealt,
            damage_received=playerStats.c.damage_received + stmt.inserted.damage_received,
            bomb_plants=playerStats.c.bomb_plants + stmt.inserted.bomb_plants,
            bomb_defuses=playerStats.c.bomb_defuses + stmt.inserted.bomb_defuses,
            playtime=playerStats.c.playtime + stmt.inserted.playtime,
            last_played=stmt.inserted.last_played,
            rounds_won=playerStats.c.rounds_won + stmt.inserted.rounds_won,
            rounds_lost=playerStats.c.rounds_lost + stmt.inserted.rounds_lost
        )



        for w in payload.weapons:
            stmtWeapon = mysql_insert(weaponStats).values(
                steamid= p.steamid,
                weapon = w.weapon,
                kills = w.kills,
                headshots = w.headshots,
                shots_hit = w.shots_hit,
                damage_dealt = w.damage_dealt
            )

            stmtWeapon = stmtWeapon.on_duplicate_key_update(
                kills=weaponStats.c.kills + stmtWeapon.inserted.kills,
                headshots=weaponStats.c.headshots + stmtWeapon.inserted.headshots,
                shots_hit=weaponStats.c.shots_hit + stmtWeapon.inserted.shots_hit,
                damage_dealt=weaponStats.c.damage_dealt + stmtWeapon.inserted.damage_dealt
            )

            conn.execute(stmtWeapon)

        conn.execute(stmt)
