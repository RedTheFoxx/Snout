-- --------------------------------------------------------
-- Version du serveur:           3.39.4
-- SE du serveur:                
-- HeidiSQL Version:             12.3.0.6589
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES  */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

-- Listage de la structure de table dynamic_data. Accounts
CREATE TABLE IF NOT EXISTS Accounts (
    AccountNumber INTEGER PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Balance REAL NOT NULL,
    Currency TEXT NOT NULL,
    OverdraftLimit REAL NOT NULL,
    InterestRate REAL NOT NULL,
    AccountFees REAL NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES Users(UserId)
);

-- Listage des donn�es de la table dynamic_data.Accounts : 0 rows
/*!40000 ALTER TABLE "Accounts" DISABLE KEYS */;
/*!40000 ALTER TABLE "Accounts" ENABLE KEYS */;

-- Listage de la structure de table dynamic_data. Actions
CREATE TABLE IF NOT EXISTS "Actions" (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT,
  value REAL
  );

-- Listage des donn�es de la table dynamic_data.Actions : 0 rows
/*!40000 ALTER TABLE "Actions" DISABLE KEYS */;
/*!40000 ALTER TABLE "Actions" ENABLE KEYS */;

-- Listage de la structure de table dynamic_data. Action_logs
CREATE TABLE IF NOT EXISTS "Action_logs" (
	"id" INTEGER NOT NULL,
	"user" INTEGER NOT NULL DEFAULT 0,
	"invokedAction" TEXT NOT NULL DEFAULT '',
	"timestamp" TEXT NOT NULL DEFAULT '',
	PRIMARY KEY ("id")
);

-- Listage des donn�es de la table dynamic_data.Action_logs : 0 rows
/*!40000 ALTER TABLE "Action_logs" DISABLE KEYS */;
/*!40000 ALTER TABLE "Action_logs" ENABLE KEYS */;

-- Listage de la structure de table dynamic_data. Transactions
CREATE TABLE IF NOT EXISTS Transactions (
    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountNumber INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Amount REAL NOT NULL,
    "DestinationAccountNumber" INTEGER NULL, "Date" TEXT NULL DEFAULT NULL,
    FOREIGN KEY (AccountNumber) REFERENCES Accounts(AccountNumber)
);

-- Listage des donn�es de la table dynamic_data.Transactions : 0 rows
/*!40000 ALTER TABLE "Transactions" DISABLE KEYS */;
/*!40000 ALTER TABLE "Transactions" ENABLE KEYS */;

-- Listage de la structure de table dynamic_data. urls
CREATE TABLE IF NOT EXISTS urls (id INTEGER PRIMARY KEY, url TEXT);

-- Listage des donn�es de la table dynamic_data.urls : 7 rows
/*!40000 ALTER TABLE "urls" DISABLE KEYS */;
/*!40000 ALTER TABLE "urls" ENABLE KEYS */;

-- Listage de la structure de table dynamic_data. Users
CREATE TABLE IF NOT EXISTS Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    DiscordId TEXT UNIQUE NOT NULL
);

-- Listage des donn�es de la table dynamic_data.Users : 0 rows
/*!40000 ALTER TABLE "Users" DISABLE KEYS */;
/*!40000 ALTER TABLE "Users" ENABLE KEYS */;

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
