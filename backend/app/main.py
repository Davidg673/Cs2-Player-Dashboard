from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.routes.players import router as players_router
from app.routes.auth import router as auth_router


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

app.include_router(players_router)
app.include_router(auth_router)

@app.get("/")
def root():
    return {"status" : "FastAPI is running"}

