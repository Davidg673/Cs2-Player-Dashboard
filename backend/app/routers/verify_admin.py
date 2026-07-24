from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from sqlalchemy import select
from sqlalchemy.exc import SQLAlchemyError
from app.db import engine
from app.models import users
from argon2 import PasswordHasher
from argon2.exceptions import VerifyMismatchError
import logging

logger = logging.getLogger(__name__)

class Login_Info(BaseModel):
    name: str
    password: str

hasher = PasswordHasher()

router = APIRouter(tags=["verify_admin"])


@router.post("/admin_login_verify")
async def verfiy_info(info: Login_Info):
    """
    Verifies user account info with database

    Args:
        info: see above for schema of http payload

    Returns:
        HTTPResponse (dict): outcome as string names (success/failure)
    """

    stmt = select(users).where(users.c.username == info.name)
    storedHash = ""

    try:
        with engine.connect() as conn:
            resultRow = conn.execute(stmt).first()
            storedHash = resultRow.password_hash

    except SQLAlchemyError as ex:
        logger.exception(str(ex))
        return HTTPException(status_code= 500, detail="An Error Occurred")

    if storedHash == "":
        return {"status":"not_found"}

    ##hasher raises error if hashes do not match, if match continue execution
    try:
        hasher.verify(storedHash, info.password)
        return {"status": "success"}
    except VerifyMismatchError: #pass doesn't match
        return {"status": "wrong_pass"}
