from datetime import  datetime

from sqlalchemy import Table, Integer, String,Column,DateTime, PrimaryKeyConstraint

from app.db import metadata

playerStats = Table( #Table signature from C# plugin must match exactly
    "player_stats",
    metadata,
    Column("steamid",String(18),primary_key=True,nullable=False),
    Column("kills",Integer,nullable=False,default=0),
    Column("deaths", Integer, nullable=False, default=0),
    Column("assists", Integer, nullable=False, default=0),
    Column("headshots", Integer, nullable=False, default=0),
    Column("damage_dealt", Integer, nullable=False, default=0),
    Column("damage_received", Integer, nullable=False, default=0),
    Column("bomb_plants", Integer, nullable=False, default=0),
    Column("bomb_defuses", Integer, nullable=False, default=0),
    Column("playtime", Integer, nullable=False, default=0),
    Column("last_played", DateTime, nullable=False, default=datetime.now),
    Column("rounds_won", Integer, nullable=False, default=0),
    Column("rounds_lost", Integer, nullable=False, default=0),

)
weaponStats = Table(
    "player_weapon_stats",
    metadata,
    Column("steamid", String(18), nullable=False ,primary_key=True),
    Column("weapon", String(18), nullable=False),
    Column("kills", Integer, nullable=False, default=0),
    Column("headshots", Integer, nullable=False, default=0),
    Column("shots_hit", Integer, nullable=False, default=0),
    Column("damage_dealt", Integer, nullable=False, default=0),
    PrimaryKeyConstraint("steamid","weapon")
)


users = Table(
    "users",
    metadata,
    Column("id",Integer,autoincrement=True, primary_key=True),
    Column("steamid",String(18),nullable=True, unique=True),
    Column("username",String(50),nullable=False),
    Column("password_hash",String(255),nullable=False),
    Column("role",String(15),nullable=False)
)