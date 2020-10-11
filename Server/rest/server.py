import json
import zipfile
import os
import jwt
import uuid
from flask import Flask, request, abort, jsonify, send_file
from datetime import datetime

from model.user import User
from model.livery import Livery
from connector import Connector
from auth import Auth

from waitress import serve


app = Flask(__name__)
app.secret_key = os.getenv('FLASK_SECRET_KEY')
download_dir = os.getenv('DOWNLOAD_DIR_ABS')

serving_port = int(os.getenv('SERVING_PORT'))


def validate_request(header):

    jwt_token = header.get('Authorization', '').encode('utf8')
    user = Auth.user_from_token(jwt_token)
    return user



@app.route('/api/v1/users/create', methods=['POST'])
def create_user():
    """create a new user
       requires: discordID, steamID

       will fail if either of it is already in use

    Returns:
        [type]: [description]
    """
    body = request.get_json()

    if body is None:
        abort(400)

    user = User(body)
    user_inserted = Connector.add_user(user)

    if user_inserted is None:
        abort(422)
    elif user_inserted == -1:
        abort(409)

    return jsonify(user_inserted.to_json()), 201



@app.route('/api/v1/users/delete')
def delete_user():
    abort(501)



@app.route('/api/v1/users/login', methods=['POST'])
def validate_credentials():
    body = request.get_json()

    if body is None:
        abort(400)

    user = User(body)

    # the result is a user on success
    # this will delete the token from ram 
    # (as posted user is not used and db user doesn't store token)
    result = Connector.validate_hash(user)

    if result is None:
        abort(403)
    elif result == -1:
        abort(404)
    elif isinstance(result, User):
        # generate jwt token
        return Auth.gen_token(result), 200
    else:
        abort(422)

    
@app.route('/api/v1/liveries')
def get_livery_list():

    user = validate_request(request.headers)
    if not user:
        abort(403)

    livs = Connector.get_liveries()

    json_list = []
    [json_list.append(liv.to_json()) for liv in livs]
    
    return jsonify(json_list)



@app.route('/api/v1/liveries', methods=['POST'])
def upload_livery():

    user = validate_request(request.headers)
    if not user:
        abort(403)


    if not os.path.exists(download_dir):
        os.mkdir(download_dir)

    filename = str(uuid.uuid4()) + '.zip'

    if not 'file' in request.files\
        or not 'Name' in request.form\
        or not 'Checksum' in request.form:

        abort(400)


    
    liv = Livery()
    liv.name = request.form['Name'][:50] # limited to 50 chars
    liv.checksum = request.form['Checksum'][:40] # limited to 40
    liv.filename = filename
    liv.owner_id = user.id

    if Connector.get_livery_by_name(liv.name):
        abort(409) # livery duplicate


    liv = Connector.add_livery(liv)
    
    data = request.files['file']
    data.save(download_dir + filename)
    

    return jsonify(liv.to_json())


@app.route('/api/v1/liveries', methods=['PUT'])
def update_livery():
    user = validate_request(request.headers)
    if not user:
        abort(403)


    if not os.path.exists(download_dir):
        os.mkdir(download_dir)


    if not 'file' in request.files\
        or not 'Name' in request.form\
        or not 'Checksum' in request.form:

        abort(400)

    # check if livery is existing, and if the user is the owner
    liv = Connector.get_livery_by_name(request.form['Name'][:50])
    if not liv:
        abort(404)

    if not liv.owner_id == user.id:
        abort(403)

    # update the checksum
    liv.checksum = request.form['Checksum'][:40] # limited to 40

    liv.insert_time = datetime.now()
    Connector.update_livery_checksum_date(liv)
    
    # replace livery with update
    os.remove(download_dir + liv.filename)

    data = request.files['file']
    data.save(download_dir + liv.filename)
    

    return jsonify(liv.to_json())




@app.route('/api/v1/liveries/<id>', methods=['GET'])
def download_livery(id):

    user = validate_request(request.headers)
    if not user:
        abort(403)

    if not id.isdigit():
        abort(400)

    id = int(id)
    liv = Connector.get_livery(id)

    if liv is None:
        abort(404)

    if not os.path.exists(download_dir + liv.filename):
        abort(404)


    return send_file(download_dir + liv.filename, attachment_filename=liv.name + '.zip', as_attachment=True)


@app.route('/api/v1/liveries/<id>', methods=['DELETE'])
def delete_livery(id):

    user = validate_request(request.headers)
    if not user:
        abort(403)

    if not id.isdigit():
        abort(400)

    id = int(id)
    liv = Connector.get_livery(id)

    if liv is None:
        abort(404)


    if not user.id == liv.owner_id:
        abort(403)

    os.remove(download_dir + liv.filename)
    Connector.del_livery(id)

    return "Deleted", 204




if __name__ == '__main__':
    #app.run()
    serve(app, host='0.0.0.0', port=serving_port)
