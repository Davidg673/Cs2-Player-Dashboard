import os

from fastapi import HTTPException

from dotenv import load_dotenv
import logging
from argon2 import PasswordHasher

from sqlalchemy.dialects.mysql import insert as mysql_insert
from sqlalchemy.exc import SQLAlchemyError
from app.models import users
from app.db import engine

load_dotenv()

logger = logging.getLogger(__name__);
hasher = PasswordHasher()

OWNER_USERNAME = os.getenv("OWNER_USERNAME")
OWNER_PASSWORD = os.getenv("OWNER_PASSWORD")



def create_owner(): 
    """
        Create or update the owner account in the db.
        Overrides db account with .env account each time
    """
    try:
        with engine.begin() as conn:
            stmt = mysql_insert(users).values(
                id=1,
                steamid=None,
                username=OWNER_USERNAME,
                password_hash=hasher.hash(OWNER_PASSWORD),
                role="owner"
            )

            stmt = stmt.on_duplicate_key_update(
                id=1,
                steamid=None,
                username=stmt.inserted.username,
                password_hash = stmt.inserted.password_hash,
                role="owner"
            )

            conn.execute(stmt)

    except SQLAlchemyError as ex:
        logger.exception(str(ex))
        raise HTTPException(status_code=500, detail= "An Error Occurred")