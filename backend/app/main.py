import os

from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.db import metadata, engine
from app.routers.players import router as players_router
from app.routers.auth import router as auth_router
from app.routers.stats import router as stats_router
from app.routers.handle_login import login_router, cookie_router


from app.services.create_owner import create_owner


load_dotenv()

app = FastAPI()



##Whitelists addresses visited by backend
origins = [origin.strip() for origin in os.getenv("CORS_ORIGINS","").split(",")]

app.add_middleware(  #Ensures local front end can call online backend due to CORS restrictions
    CORSMiddleware,
    allow_origins = origins,
    allow_credentials = True,
    allow_methods = ["GET", "POST", "OPTIONS"],
    allow_headers = ["*"],
)

app.include_router(players_router)
app.include_router(auth_router)
app.include_router(stats_router)
app.include_router(cookie_router)
app.include_router(login_router)

##Create Database
metadata.create_all(bind=engine)

##Create site owner
create_owner()

@app.get("/")
def root():
    return {"status" : "FastAPI is running"}

