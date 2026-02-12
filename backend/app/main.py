import os

from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.db import metadata, engine
from app.routes.players import router as players_router
from app.routes.auth import router as auth_router
from app.routes.stats import router as stats_router


app = FastAPI()

load_dotenv()

raw_origins = os.getenv("CORS_ORIGINS","")
origins = [ ##Whitelists addresses visited by backend
    o.strip() for o in raw_origins.split(",") if o
]

app.add_middleware(  #Ensures local front end can call online backend due to CORS restrictions
    CORSMiddleware,
    allow_origins = origins,
    allow_credentials = True,
    allow_methods = ["*"],
    allow_headers = ["*"],
)

app.include_router(players_router)
app.include_router(auth_router)
app.include_router(stats_router)

##Create Database
metadata.create_all(bind=engine)


@app.get("/")
def root():
    return {"status" : "FastAPI is running"}

