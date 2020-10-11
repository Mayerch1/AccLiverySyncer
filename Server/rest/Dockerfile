FROM python:3

RUN apt-get update
RUN pip3 install Flask waitress mysql-connector-python pyjwt bcrypt


RUN mkdir -p /server

ADD . server/
# COPY server.py /server/server.py



WORKDIR /server

ENV FLASK_APP=server.py
CMD ["python", "server.py"]