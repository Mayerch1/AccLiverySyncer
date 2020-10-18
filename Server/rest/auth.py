import jwt
import os
from datetime import datetime, timedelta

from connector import Connector
from model.user import User

class Auth:

    secret = os.getenv('AUTH_SECRET_KEY')



    @staticmethod
    def gen_token(user: User):

        expires = (datetime.now() + timedelta(hours=12)).timestamp()
        token = jwt.encode({'Id': user.id, 'Expires': expires}, Auth.secret, algorithm='HS256')
        
        return token



    def user_from_token(token: str):

        try:
            decoded = jwt.decode(token, Auth.secret, algorithms=['HS256'])
        except jwt.DecodeError:
            return None

        if 'Expires' not in decoded\
            or 'Id' not in decoded\
            or datetime.fromtimestamp(decoded['Expires']) < datetime.now():

            return None

        
        user = Connector.get_user(decoded['Id'])
        return user

