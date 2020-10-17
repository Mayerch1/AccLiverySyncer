import os
import secrets
import bcrypt
import mysql.connector

from model.user import User
from model.livery import Livery


class Connector:

    _conn = None
    _cursor = None


    @staticmethod
    def _get_cursor():
        Connector._conn = mysql.connector.connect(
                host = os.getenv('SQL_ADDRESS'),
                port = int(os.getenv('SQL_PORT')),
                user = "serverScript",
                password = os.getenv('SQL_PASSWORD'),
                database ='AccLiveries'
                )
        

        Connector._cursor = Connector._conn.cursor()
        return Connector._cursor
        

    @staticmethod
    def _close_connection():
        Connector._conn.close()


    @staticmethod
    def _commit_connection():
        Connector._conn.commit()
        Connector._conn.close()


    @staticmethod
    def _get_hashed_password(plain_text_password):
        # Hash a password for the first time
        #   (Using bcrypt, the salt is saved into the hash itself)
        return bcrypt.hashpw(plain_text_password.encode('utf8'), bcrypt.gensalt())


    @staticmethod
    def _check_password(plain_text_password, hashed_password):
        # Check hashed password. Using bcrypt, the salt is saved into the hash itself
        return bcrypt.checkpw(plain_text_password.encode('utf8'), hashed_password.encode('utf8'))


    @staticmethod
    def get_user(id: int):
        cursor = Connector._get_cursor()
        cursor.execute('''select * from User where ID=%d''' % (id))

        user = cursor.fetchone()

        Connector._close_connection()

        if user is None:
            return None
        else:
            return User(user)


    @staticmethod
    def add_user(user: User):
        cursor = Connector._get_cursor()

        cursor.execute('''select * from User where DiscordId = %d''' % (user.discord_id))
        result = cursor.fetchone()

       

        # user is already registered
        if result is not None:
            return -1


        # get the token for the user (Tokene)
        # limit to database size
        user.token = secrets.token_urlsafe(70)[:70]

        # !!! never transmitt only for db
        _hash = Connector._get_hashed_password(user.token).decode()


        # insert new user into db
        cursor.execute('''insert INTO User(SteamId, DiscordId, Hash) VALUES(%d, %d, "%s")''' % (user.steam_id, user.discord_id, _hash))
        id = cursor.lastrowid

        Connector._commit_connection()

        user.id = id
        return user


    @staticmethod
    def validate_hash(user: User):
        cursor = Connector._get_cursor()

        if user.discord_id is None:
            return None

        # currently only login over discord is supported
        cursor.execute('''select * from User where DiscordId = %d''' % (user.discord_id))
        result = cursor.fetchone()

        Connector._close_connection()

        # user is already registered
        if result is None:
            return -1

        db_user = User(result)

        if Connector._check_password(user.token, db_user._hash):
            return db_user
        else:
            return None



    @staticmethod
    def get_liveries():
        cursor = Connector._get_cursor()

        cursor.execute('''select * from Livery''')
        result = cursor.fetchall()

        Connector._close_connection()

        if result is None:
            return None

        livs = []
        for l in result:
            livs.append(Livery(l))

        return livs


    @staticmethod
    def add_livery(liv: Livery):
        cursor = Connector._get_cursor()

        # livery must have valid members
        cursor.execute('''insert INTO Livery(Checksum, Owner, Filename, Name) VALUES("%s", %d, "%s", "%s")''' % (liv.checksum, liv.owner_id, liv.filename, liv.name))
        id = cursor.lastrowid

        Connector._commit_connection()

        liv.id = id
        return liv



    @staticmethod
    def get_livery_by_name(name: str):
        cursor = Connector._get_cursor()

        cursor.execute('''select * from Livery where Name="%s"''' % (name))


        liv = cursor.fetchone()

        Connector._close_connection()

        if liv is None:
            return None
        else:
            return Livery(liv)

    @staticmethod
    def get_livery(id: int):
        cursor = Connector._get_cursor()
        cursor.execute('''select * from Livery where ID=%d''' % (id))

        liv = cursor.fetchone()

        Connector._close_connection()

        if liv is None:
            return None
        else:
            return Livery(liv)


    @staticmethod
    def del_livery(id: int):
        cursor = Connector._get_cursor()
        cursor.execute('''delete from Livery where ID=%d''' % (id))

        Connector._commit_connection()


    @staticmethod
    def update_livery_checksum_date(liv: Livery):

        date_formatted = liv.insert_time.strftime('%Y-%m-%d %H:%M:%S')

        cursor = Connector._get_cursor()
        cursor.execute('''update Livery SET Checksum="%s", InsertTime="%s" where ID=%d''' % (liv.checksum, date_formatted, liv.id))

        Connector._commit_connection()
        
