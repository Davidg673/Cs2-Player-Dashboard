import os
from dotenv import load_dotenv
import logging
from argon2 import PasswordHasher

from sqlalchemy.dialects.mysql import insert as mysql_insert
from sqlalchemy.exc import SQLAlchemyError
from app.models import users
from app.db import engine

load_dotenv()

logger = logging.getLogger("create_owner");
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
                role="admin"
            )

            stmt = stmt.on_duplicate_key_update(
                id=1,
                steamid=None,
                username=stmt.inserted.username,
                password_hash = stmt.inserted.password_hash,
                role="admin"
            )

            conn.execute(stmt)

    except SQLAlchemyError:
        logger.exception("DB operation failed")
        raise
