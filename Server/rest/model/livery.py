class Livery:
    id = None

    insert_time = None
    checksum = None
    name = None

    # not transmitted, but not a secret either
    filename = None

    # foreign key to TrackData
    owner_id = None
    owner = None



    def __init__(self, raw_input = None):

        # input from database
        if isinstance(raw_input, tuple):
            self.parse_from_tuple(raw_input)

        # input from rest
        elif isinstance(raw_input, dict):
            self.parse_from_json(raw_input)
            pass


    def parse_from_tuple(self, db_tuple):
        self.id = db_tuple[0]
        self.insert_time = db_tuple[1]
        self.checksum = db_tuple[2]

        self.filename = db_tuple[3]
        self.name = db_tuple[4]
        self.owner_id = db_tuple[5]



    def parse_from_json(self, db_dict):
        self.id = db_dict.get('Id', None)
        self.insert_time = db_dict.get('InsertTime', None)
        self.checksum = db_dict.get('Checksum', None)
        self.filename = db_dict.get('Filename', None)
        self.name = db_dict.get('Name', None)
        self.owner_id = db_dict.get('OwnerId', None)

        # do not resolve foreign key for now
        



    def to_json(self):
        d = dict({
            'Id': self.id,
            'InsertTime': self.insert_time,
            'Checksum': self.checksum,
            'Name': self.name,
            'OwnerId': self.owner_id
            
        })

        return d
