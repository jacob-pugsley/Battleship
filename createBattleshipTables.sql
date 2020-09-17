create table game(
	gameId int,
    player1Id int,
    player2Id int,
    currentPlayer int,
    victory bool default false,
    primary key(gameId)
);

create table shipNames(
	id int,
    shipName varchar(20),
    primary key(id)
);

create table hitPoints(
	id int,
    gameId int,
    playerId int,
    xPos int,
    yPos int,
    hit boolean,
    foreign key(gameId) references game(gameId),
    primary key(id)
);



create table ship_hitPoints(
	gameId int,
	shipId int,
    hitPointId int unique,
    foreign key(shipId) references shipNames(id),
    foreign key(hitPointId) references hitPoints(id),
    foreign key(gameId) references game(gameId)
);

create table misses(
	gameId int,
    playerId int,
    xPos int,
    yPos int,
    foreign key(gameId) references game(gameId)
);

