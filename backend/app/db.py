import os

from dotenv import load_dotenv
from sqlalchemy import create_engine,MetaData

load_dotenv()

DB_HOST = os.getenv("DB_HOST")
DB_PORT = os.getenv("DB_PORT","3306")
DB_NAME = os.getenv("DB_NAME")
DB_USER = os.getenv("DB_USER")
DB_PASSWORD = os.getenv("DB_PASSWORD")

DB_URL =(
    f"mysql+pymysql://{DB_USER}:{DB_PASSWORD}"
    f"@{DB_HOST}:{DB_PORT}/{DB_NAME}"
)
##Creates a list of missing env values by creating a dict from the env values collected, iterating through it and selecting for keys, and returns if not value is found
missing = [k for k,v in {
    "DB_HOST": DB_HOST,
    "DB_USER": DB_USER,
    "DB_NAME": DB_NAME,
    "DB_PASSWORD": DB_PASSWORD
}.items() if not v]

##Ensures env is set up correctly before continuing execution
if missing:
    RuntimeError(f"Missing env variables: {','.join(missing)}")


engine = create_engine(DB_URL,
                       pool_pre_ping=True,
                       connect_args={"connect_timeout": 5,"read_timeout": 5 })

metadata = MetaData()
