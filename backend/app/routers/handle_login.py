import os
from dotenv import load_dotenv

from fastapi import HTTPException, APIRouter, Response, Request
from jose import jwt,JWTError
from datetime import datetime, timedelta,timezone

from pydantic import BaseModel
from ..models import users
from ..db import engine
from sqlalchemy import select,Row
from sqlalchemy.exc import SQLAlchemyError

from  argon2 import PasswordHasher
from argon2.exceptions import VerifyMismatchError

hasher = PasswordHasher()

load_dotenv()

SECRET_KEY = os.getenv("INGEST_API_KEY")
ALGORITHM = "HS256"

login_router = APIRouter(tags=["login"])
cookie_router = APIRouter(tags=["cookie"])



""" Pydantic Model for login so api can unpack in function below"""
class LoginRequest(BaseModel):
    username: str
    password: str

@login_router.post("/login")
def login(data: LoginRequest, response: Response):
    """
    Creates cookie so user can access site and sets it in the HTTP response back to the client. Cookie validation is done in api below and gets checked at /admin root to validate user

        data: pydantic model for username,password
        response: FastAPI provided object which will be sent back to client as a result
    """

    user = authenticate_user(data.username, data.password)
    if not user:
        raise HTTPException(status_code=401,detail="Invalid Credentials")

    if user.role not in ["admin","owner"]:
        raise HTTPException(status_code=401,detail="Unauthorized Access")


    token = create_token({
        "username":data.username,
        "role":user.role
    })

    ####Sets cookie in response to client
    response.set_cookie( 
        key="auth_token",
        value=token,
        httponly=True,
        samesite="none",
        secure=True
    )

    return {"success":True}



def authenticate_user(username:str, password:str) -> Row | None:
    """
    Queries db for username, returns and compares password hashes
    """
    try:
        with engine.connect() as conn:
            stmt = select(users).where(users.c.username==username)

            result = conn.execute(stmt).fetchone() 

    except SQLAlchemyError as err:
        print(err)

        raise HTTPException(
            status_code=500,
            detail="Database error"
        )
    
    if not result:
        return None
    
    
    try:
        hasher.verify(result.password_hash,password)
        return result
    except VerifyMismatchError:
        return None







@cookie_router.get("/verify_cookie")
def verify_cookie(request: Request):

    token = request.cookies.get("auth_token") #Get cookie from request

    if not token:
        raise HTTPException(status_code=401, detail="Not logged in")
    
    try:
        payload = jwt.decode(token,SECRET_KEY, algorithms=[ALGORITHM]) ##jwt function to check signature, expiry and return original stored data
        return {"user":payload} #if valid return user
    except JWTError:
        raise HTTPException(status_code=401, detail="Invalid Token")



"""Creates signed token (JWT) that proves who user is. Data is username/role"""
def create_token(data: dict):
    to_encode = data.copy()
    to_encode["exp"] = datetime.now(timezone.utc) + timedelta(hours=1) #Expiry time for token
    return jwt.encode(to_encode,SECRET_KEY,algorithm=ALGORITHM) #Encode (Sign) to produce a usable string
