from sqlalchemy.exc import SQLAlchemyError, OperationalError


## Used to mimic fetchall() call, returns empty arr
class FakeWeaponsResult:
    def fetchall(self):
        return []

## Used to mimic frist() call on connection context manager
class FakePlayerResult:
    def first(self):
        return None

class FakeWeaponsResultValid:
    def fetchall(self):
        return [
            _FakeRow({
                "steamid":"12345",
                "weapon":"ak47",
                "kills":7,
                "headshots":2,
                "shots_hit":30,
                "damage_dealt":800
            }),
            _FakeRow({
                "steamid": "12345",
                "weapon": "deagle",
                "kills": 3,
                "headshots": 1,
                "shots_hit": 10,
                "damage_dealt": 400
            })
        ]
#Required to mimic SqlAlchemy _mapping object
class _FakeRow:
    def __init__(self,mapping):
        self._mapping=mapping

## Used to mimic frist() call on connection context manager
class FakePlayerResultValid:
    def first(self):
        return (
            "12345", #steamid
            10, #kills
            5, #deaths
            2, #assists
            3, #headshots
            1200, #damage_dealt
            900, #damage_received
            1, #bomb_plants
            0, #bomb defuses
            3600, #playtime
            None, #last_played
            8, #rounds_won
            9 #rounds_lost
        )



"""
These should match the conn object with the execute method
first call returns player and next call returns weapon **objects**
to match methods in players.py players require first() and weapons fetchall()
"""
class FakeConnPlayers:
    def __init__(self,results):
         self.results = list(results)

    #### executes given instances of the classes above in order
    def execute(self,cmd):
        return self.results.pop(0)


class FakeConnStats:
    def execute(self,stmt):
        return None

"""
`with engine.connect() as conn` wraps bellow class in temp variable : `temp = engine.connect()`
so then conn=temp.__enter__() and exit which match the methods below. These are requried to complete
the chain of execution until invalid player id is returned
"""
class FakeConnectCtx:
    def __init__(self,conn):
        self.conn = conn

    def __enter__(self):
        return self.conn

    def __exit__(self, exc_type, exc_val, exc_tb):
        return False


"""
Fake engine to replace sqlAlchemy engine used to query steamID.
Returns context manager _FakeConnectCtx
"""
class FakeEngine:
    def __init__(self,conn):
        self.conn = conn

    def connect(self):
        return FakeConnectCtx(self.conn)

    def begin(self):
        return FakeConnectCtx(self.conn)


"""
Other Engines which directly raise errors
"""

class EngineRaisesSQLAlchemy:
    def connect(self):
        raise SQLAlchemyError("boom")

class EngineRaisesOperational:
    def connect(self):
        raise OperationalError("stmt", {},"orig")

