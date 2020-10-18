drop database AccLiveries;
create database if not exists `AccLiveries`;

use AccLiveries;

show tables;


create table if not exists
`AccLiveries`.`User`(
	`ID` INT not null auto_increment,
    `SteamId` BIGINT,
	`DiscordId` varchar(40),
	`Hash` varchar(70) not null,
    PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;

create table if not exists
`AccLiveries`.`Livery`(
	`ID` INT not null auto_increment,
    `InsertTime` datetime not null default CURRENT_TIMESTAMP,
	`Checksum` varchar(40) not null,
    `FileName` varchar(100),
    `Name` varchar(50),
    `Owner` int not null,        
    PRIMARY KEY (`ID`),
    CONSTRAINT `fk_Song_User`
		FOREIGN KEY (`Owner`)
        REFERENCES `AccLiveries`.`User` (`ID`)
        On DELETE no ACTION
        ON UPDATE no ACTION
)
ENGINE = InnoDB;



create user if not exists'serverScript' identified by '***';
grant select on `AccLiveries`.`User` to 'serverScript';
grant insert on `AccLiveries`.`User` to 'serverScript';
grant delete on `AccLiveries`.`User` to 'serverScript';

grant select on `AccLiveries`.`Livery` to 'serverScript';
grant insert on `AccLiveries`.`Livery` to 'serverScript';
grant delete on `AccLiveries`.`Livery` to 'serverScript';
grant update on `AccLiveries`.`Livery` to 'serverScript';