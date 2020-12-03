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
    gameId int,
    foreign key(gameId) references game(gameId) on delete cascade,
    primary key(id)
);

create table hitPoints(
	id int,
    gameId int,
    playerId int,
    xPos int,
    yPos int,
    hit boolean,
    foreign key(gameId) references game(gameId) on delete cascade,
    primary key(id)
);



create table ship_hitPoints(
	gameId int,
	shipId int,
    hitPointId int unique,
    foreign key(shipId) references shipNames(id) on delete cascade,
    foreign key(hitPointId) references hitPoints(id) on delete cascade,
    foreign key(gameId) references game(gameId) on delete cascade
);

create table misses(
	gameId int,
    playerId int,
    xPos int,
    yPos int,
    foreign key(gameId) references game(gameId) on delete cascade
);

create table users(
	playerId int,
    username varchar(50),
    email varchar(100),
    wins int,
    losses int,
    primary key(playerId)
);
