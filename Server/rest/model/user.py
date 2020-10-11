class User:
    # currently not transmitted
    # but not 'secret' either
    id = None

    steam_id = None
    discord_id = None
    
    # api token, cannot be reset by user
    token = None

    # !!! never transmitt !!!
    _hash = None



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
        self.steam_id = db_tuple[1]
        self.discord_id = db_tuple[2]
        self._hash = db_tuple[3]



    def parse_from_json(self, db_dict):
        self.id = db_dict.get('id', None)
        self.steam_id = db_dict.get('SteamId', None)
        self.discord_id = db_dict.get('DiscordId', None)
        self.token = db_dict.get('Token', None)
        



    def to_json(self):
        d = dict({
            'SteamId': self.steam_id,
            'DiscordId': self.discord_id,
            'Token': self.token,
            
        })

        return d
