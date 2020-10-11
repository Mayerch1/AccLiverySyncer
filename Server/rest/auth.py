import jwt
import os
from datetime import datetime, timedelta

from connector import Connector
from model.user import User

class Auth:

    secret = os.getenv('AUTH_SECRET_KEY')

    # save the token twice
    # once for easy indexed access (at login)
    # once for user-based access (for invalidating old tokens)
    token_list = []
    user_dict = {}


    @staticmethod
    def gen_token(user: User):

        if user.id in Auth.user_dict:
            Auth.token_list.remove(Auth.user_dict[user.id])
            del Auth.user_dict[user.id]

        expires = (datetime.now() + timedelta(days=7)).timestamp()
        
        token = jwt.encode({'Id': user.id, 'Expires': expires}, Auth.secret, algorithm='HS256')

        Auth.token_list.append(token)
        Auth.user_dict[user.id] = token

        
        return token



    def user_from_token(token: str):

        if not token in Auth.token_list:
            return  None

        decoded = jwt.decode(token, Auth.secret, algorithms=['HS256'])

        if 'Expires' not in decoded\
            or 'Id' not in decoded\
            or datetime.fromtimestamp(decoded['Expires']) < datetime.now():

            return None

        
        user = Connector.get_user(decoded['Id'])
        return user

