from sqlalchemy import create_engine,MetaData


engine = create_engine("mysql+pymysql://server:changeme@127.0.0.1:3306/db",
                       pool_pre_ping=True,
                       connect_args={"connect_timeout": 5,"read_timeout": 5 })

metadata = MetaData()
