/*
SQLyog Community v11.31 (32 bit)
MySQL - 5.6.12-log : Database - programmingchallengedb
*********************************************************************
*/

/*!40101 SET NAMES utf8 */;

/*!40101 SET SQL_MODE=''*/;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE DATABASE /*!32312 IF NOT EXISTS*/`programmingchallengedb` /*!40100 DEFAULT CHARACTER SET latin1 */;

USE `programmingchallengedb`;

/*Table structure for table `asp_membershiproles` */

DROP TABLE IF EXISTS `asp_membershiproles`;

CREATE TABLE `asp_membershiproles` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(64) DEFAULT NULL,
  `ApplicationName` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1;

/*Data for the table `asp_membershiproles` */

LOCK TABLES `asp_membershiproles` WRITE;

insert  into `asp_membershiproles`(`Id`,`RoleName`,`ApplicationName`) values (1,'Admin','Challenge');

UNLOCK TABLES;

/*Table structure for table `asp_membershipusers` */

DROP TABLE IF EXISTS `asp_membershipusers`;

CREATE TABLE `asp_membershipusers` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Username` varchar(64) NOT NULL,
  `ApplicationName` varchar(64) NOT NULL,
  `Email` varchar(128) NOT NULL,
  `Comment` varchar(255) DEFAULT NULL,
  `Password` varchar(64) NOT NULL,
  `PasswordQuestion` varchar(255) DEFAULT NULL,
  `PasswordAnswer` varchar(255) DEFAULT NULL,
  `IsApproved` bit(1) DEFAULT NULL,
  `LastActivityDate` datetime DEFAULT NULL,
  `LastLoginDate` datetime DEFAULT NULL,
  `LastPasswordChangedDate` datetime DEFAULT NULL,
  `CreationDate` datetime DEFAULT NULL,
  `IsOnLine` bit(1) DEFAULT NULL,
  `IsLockedOut` bit(1) DEFAULT NULL,
  `LastLockedOutDate` datetime DEFAULT NULL,
  `FailedPasswordAttemptCount` int(11) DEFAULT NULL,
  `FailedPasswordAttemptWindowStart` datetime DEFAULT NULL,
  `FailedPasswordAnswerAttemptCount` int(11) DEFAULT NULL,
  `FailedPasswordAnswerAttemptWindowStart` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=latin1;

/*Data for the table `asp_membershipusers` */

LOCK TABLES `asp_membershipusers` WRITE;

insert  into `asp_membershipusers`(`Id`,`Username`,`ApplicationName`,`Email`,`Comment`,`Password`,`PasswordQuestion`,`PasswordAnswer`,`IsApproved`,`LastActivityDate`,`LastLoginDate`,`LastPasswordChangedDate`,`CreationDate`,`IsOnLine`,`IsLockedOut`,`LastLockedOutDate`,`FailedPasswordAttemptCount`,`FailedPasswordAttemptWindowStart`,`FailedPasswordAnswerAttemptCount`,`FailedPasswordAnswerAttemptWindowStart`) values (2,'yofret2','Challenge','riosmerca282@gmail.com','','ChEi9ogEi7oaqkNjGkga7C1Ba0Q=','','1FIFysarg2dSEt8bZhM1udtgk2g=','','2014-02-14 08:41:25','2014-02-14 11:21:26','2014-02-14 08:41:25','2014-02-14 08:41:25','\0','\0','2014-02-14 08:41:25',0,'2014-02-14 08:41:25',0,'2014-02-14 08:41:25'),(3,'yofret3','Challenge','riosmerca2822@gmail.com','','ChEi9ogEi7oaqkNjGkga7C1Ba0Q=','','1FIFysarg2dSEt8bZhM1udtgk2g=','','2014-02-14 11:19:07','0001-01-01 00:00:00','2014-02-14 11:19:07','2014-02-14 11:19:07','\0','\0','2014-02-14 11:19:07',0,'2014-02-14 11:19:07',0,'2014-02-14 11:19:07');

UNLOCK TABLES;

/*Table structure for table `asp_membershipusersinroles` */

DROP TABLE IF EXISTS `asp_membershipusersinroles`;

CREATE TABLE `asp_membershipusersinroles` (
  `Users_Id` int(11) DEFAULT NULL,
  `Roles_Id` int(11) DEFAULT NULL,
  KEY `FK_UsersInRoles_Roles` (`Roles_Id`),
  KEY `FK_UsersInRoles_Users` (`Users_Id`),
  CONSTRAINT `FK_UsersInRoles_Roles` FOREIGN KEY (`Roles_Id`) REFERENCES `asp_membershiproles` (`Id`),
  CONSTRAINT `FK_UsersInRoles_Users` FOREIGN KEY (`Users_Id`) REFERENCES `asp_membershipusers` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*Data for the table `asp_membershipusersinroles` */

LOCK TABLES `asp_membershipusersinroles` WRITE;

UNLOCK TABLES;

/*Table structure for table `entries` */

DROP TABLE IF EXISTS `entries`;

CREATE TABLE `entries` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `creationDate` datetime DEFAULT NULL,
  `title` varchar(255) DEFAULT NULL,
  `content` varchar(255) DEFAULT NULL,
  `user_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `entries_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=39 DEFAULT CHARSET=latin1;

/*Data for the table `entries` */

LOCK TABLES `entries` WRITE;

insert  into `entries`(`id`,`creationDate`,`title`,`content`,`user_id`) values (1,'2013-11-29 15:53:52','misa de sanación','Ir a rezar por la familia y amigos.',5),(2,'2013-11-30 01:42:17','ghghg','bnbnbnbn',5),(3,'2013-11-30 01:44:06','La ultima cena','Una gran Pintura.',5),(4,'2013-11-30 01:46:40','galaxy tab','Una gran linea de dispositivos mobiles.',5),(5,'2013-11-30 01:48:00','reggaeton','Me gusta este tipo de musica.',4),(6,'2013-11-30 01:48:38','futbol','El mejor equipo es Real Madrid. Hala Madrid!!',4),(7,'2013-11-30 01:49:07','Estudios','Ingenieria civil!!',4),(8,'2013-11-30 01:49:41','flaco','Estoy llevaooo!',4),(10,'2013-12-01 18:34:21','enviar tv','enviar tv a la abuela.',5),(11,'2013-12-01 18:36:14','Pacientes','Llamar pacientes para confirmar citas.',6),(12,'2013-12-01 18:36:53','clinica','Realizar clínica Lunes a las 7:00 am',6),(13,'2013-12-01 18:37:21','uñas','pintarme las uñas de diferentes colores.',6),(14,'2013-12-01 18:39:29','Marc Anthony','Comprar boletas para concierto Marc Anthony',6),(15,'2013-12-01 19:59:07','Video juegos','Jugar todo los fines de semana.',4),(16,'2013-12-01 20:54:38','PES','Instalar PES en computador y jugar con compañeros! Pes 2014',7),(17,'2013-12-01 23:35:49','viaje familiar','viaje familiar al final de año.',5),(18,'2013-12-02 02:03:37','prueba','orem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, ',5),(19,'2013-12-02 05:37:17','Lorem ipsum','Nulla pharetra eget nibh sed tempus. Aliquam commodo commodo urna ac feugiat. Etiam non consectetur odio. Cras luctus tristique elit sit amet volutpat. Nullam sit amet lacus lorem. Maecenas et tempor ipsum, sed semper odio. Praesent suscipit augue sit ame',7),(20,'2013-12-02 06:11:44','this is a test','Nam vitae neque in est malesuada vulputate non sed lectus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris nec convallis nisi. Donec pellentesque suscipit dictum.',4),(21,'2014-01-31 20:07:40','Mmmm','holis asdasdas',9),(22,'2014-01-31 20:22:36','hellou','tu me oyes tu me oyes',10),(23,'2014-02-04 19:12:36','prueba +1','asdasd',9),(38,'2008-02-06 00:00:00','LO EDITO NUEVAMENTE :D','this thing is woring like a charm :3',9);

UNLOCK TABLES;

/*Table structure for table `hidden_tweets` */

DROP TABLE IF EXISTS `hidden_tweets`;

CREATE TABLE `hidden_tweets` (
  `id` varchar(255) NOT NULL,
  `user_id` int(11) DEFAULT NULL,
  `hidden` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `hidden_tweets_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*Data for the table `hidden_tweets` */

LOCK TABLES `hidden_tweets` WRITE;

insert  into `hidden_tweets`(`id`,`user_id`,`hidden`) values ('422220339868098560',9,0),('422221141395374080',9,0),('422403771994734592',9,0),('428166711578083330',9,0),('428362339466084352',9,0),('428363356911968256',9,0),('428951993651298304',9,0),('429281591421304832',9,0),('430709133751627777',9,0);

UNLOCK TABLES;

/*Table structure for table `users` */

DROP TABLE IF EXISTS `users`;

CREATE TABLE `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(255) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `password` varchar(64) DEFAULT NULL,
  `salt` longblob,
  `twitterAccount` varchar(255) DEFAULT NULL,
  `userMembership_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  KEY `membership_userId` (`userMembership_id`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`userMembership_id`) REFERENCES `asp_membershipusers` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=latin1;

/*Data for the table `users` */

LOCK TABLES `users` WRITE;

insert  into `users`(`id`,`username`,`email`,`password`,`salt`,`twitterAccount`,`userMembership_id`) values (1,'watson','jafn28@gmail.com','—áäéÝäžÅå`ôáÓ{YŽR°uÉ\\!¡\n4nòž','�⍟\n�AV�$L�����6̣������Y','jafn28',NULL),(2,'pipa','jafn28@gmail.com','ïaµº0,Î\ZÀ*Ÿ×ö|õ,Ä¯Uq\r}ÖN.ÌJ1','v�N���V�<c{[(��3��*�S\n:�jH�','tifis',NULL),(3,'jqhuman','jafn28@gmail.com','*åil{cþ]ÞãßLŽg”ÜMp;€Ráëç#ûk,ð','�*ՀƜ\\x��P��I�L�a���Ɗ�À�if','condesa_sama',NULL),(4,'rafael','jafn28@gmail.com','ûB5J©;¶£Ü“‰ýR²(lH ¬¨ü…¶ºIY™ù','.i\\��N �|�����Z�&�l���U��`�','petrogustavo',NULL),(5,'fanny','fanny@example.com','{ÄNžÿüî%ÝT{t\0£žÈŒ›¹dö\nÉÒ›Eo','��#��@�\n�V3�#�v��+I��Ҏח$','AlvaroUribeVel',NULL),(6,'vanessa','v.puello@gmail.com','È1ß!´zëý<B‰qŒòŸ­üWÅŠÂ\'QÿOD','�y̿c���ޔ��f�@o�dy.�B/��','VanePuello',NULL),(7,'fonta','example@example.com','A@ŠïDgüTJ~àÅ¢9PÈSwÛ-íŒò¿Uè','���+A�ї+���/-,O5�a�Ȧf��z�G�','nachFont21',NULL),(8,'asdfg','example@example.com','@0T]w#\0thú\rý.¸T»šnà üaÝ˜½’Ày¼',':�A>*(����kt�^�Hs��u��V�6N=ˤ','jaja',NULL),(9,'yofret','riosmerca28@gmail.com','¿pajòÁ§ˆgoæÞžšt.»»X‹\rê\'Û%ŸæB«y','�uҷ�ȳ�\'���\\w75m���V���\Z','yriosmerca',NULL),(10,'test','riosmerca28@hotmail.com','{Ç|tè²T£k‰`•pGüZS†±®ûQµ‰Î”\\0','Tŷ.0!�p����t�\0�J@�W��J��.��','yuseidis',NULL),(11,'yofret2','riosmerca282@gmail.com','ChEi9ogEi7oaqkNjGkga7C1Ba0Q=','System.Byte[]','yriosmerca',2),(12,'yofret3','riosmerca2822@gmail.com','ChEi9ogEi7oaqkNjGkga7C1Ba0Q=','','yriosmerca',3);

UNLOCK TABLES;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
