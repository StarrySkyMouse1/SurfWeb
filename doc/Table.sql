
-- shavit.cptimes 定义

CREATE TABLE `cptimes` (
  `style` tinyint NOT NULL,
  `track` tinyint NOT NULL DEFAULT '0',
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `checkpoint` tinyint NOT NULL,
  `auth` int NOT NULL,
  `time` float NOT NULL,
  `stage_time` float NOT NULL,
  `attempts` smallint NOT NULL,
  PRIMARY KEY (`style`,`track`,`auth`,`map`,`checkpoint`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.cpwrs 定义

CREATE TABLE `cpwrs` (
  `style` tinyint NOT NULL,
  `track` tinyint NOT NULL DEFAULT '0',
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `checkpoint` tinyint NOT NULL,
  `auth` int NOT NULL,
  `time` float NOT NULL,
  `stage_time` float NOT NULL,
  `attempts` smallint NOT NULL,
  PRIMARY KEY (`style`,`track`,`map`,`checkpoint`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.maptiers 定义

CREATE TABLE `maptiers` (
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `tier` int NOT NULL DEFAULT '1',
  `maxvelocity` float NOT NULL DEFAULT '3500',
  PRIMARY KEY (`map`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.mapzones 定义

CREATE TABLE `mapzones` (
  `id` int NOT NULL AUTO_INCREMENT,
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `type` int DEFAULT NULL,
  `corner1_x` float DEFAULT NULL,
  `corner1_y` float DEFAULT NULL,
  `corner1_z` float DEFAULT NULL,
  `corner2_x` float DEFAULT NULL,
  `corner2_y` float DEFAULT NULL,
  `corner2_z` float DEFAULT NULL,
  `destination_x` float NOT NULL DEFAULT '0',
  `destination_y` float NOT NULL DEFAULT '0',
  `destination_z` float NOT NULL DEFAULT '0',
  `track` int NOT NULL DEFAULT '0',
  `flags` int NOT NULL DEFAULT '0',
  `data` int NOT NULL DEFAULT '0',
  `speedlimit` tinyint NOT NULL DEFAULT '1',
  `form` tinyint DEFAULT NULL,
  `target` varchar(63) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=11552 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.migrations 定义

CREATE TABLE `migrations` (
  `code` tinyint NOT NULL,
  PRIMARY KEY (`code`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.startpositions 定义

CREATE TABLE `startpositions` (
  `auth` int NOT NULL,
  `track` tinyint NOT NULL,
  `stage` tinyint NOT NULL,
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `pos_x` float DEFAULT NULL,
  `pos_y` float DEFAULT NULL,
  `pos_z` float DEFAULT NULL,
  `ang_x` float DEFAULT NULL,
  `ang_y` float DEFAULT NULL,
  `ang_z` float DEFAULT NULL,
  `angles_only` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`auth`,`track`,`map`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.styleplaytime 定义

CREATE TABLE `styleplaytime` (
  `auth` int NOT NULL,
  `style` tinyint NOT NULL,
  `playtime` float NOT NULL,
  PRIMARY KEY (`auth`,`style`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.users 定义

CREATE TABLE `users` (
  `auth` int NOT NULL,
  `name` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ip` int DEFAULT NULL,
  `firstlogin` int NOT NULL DEFAULT '-1',
  `lastlogin` int NOT NULL DEFAULT '-1',
  `points` float NOT NULL DEFAULT '0',
  `playtime` float NOT NULL DEFAULT '0',
  PRIMARY KEY (`auth`) USING BTREE,
  KEY `points` (`points`) USING BTREE,
  KEY `firstlogin` (`firstlogin`) USING BTREE,
  KEY `lastlogin` (`lastlogin`) USING BTREE,
  KEY `ip` (`ip`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.chat 定义

CREATE TABLE `chat` (
  `auth` int NOT NULL,
  `name` int NOT NULL DEFAULT '0',
  `ccname` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `message` int NOT NULL DEFAULT '0',
  `ccmessage` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ccaccess` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`auth`) USING BTREE,
  CONSTRAINT `ch_auth` FOREIGN KEY (`auth`) REFERENCES `users` (`auth`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.playertimes 定义

CREATE TABLE `playertimes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `style` tinyint NOT NULL DEFAULT '0',
  `track` tinyint NOT NULL DEFAULT '0',
  `time` float NOT NULL,
  `auth` int DEFAULT NULL,
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `points` float NOT NULL DEFAULT '0',
  `jumps` int DEFAULT NULL,
  `date` int DEFAULT NULL,
  `strafes` int DEFAULT NULL,
  `sync` float DEFAULT NULL,
  `perfs` float DEFAULT '0',
  `completions` smallint DEFAULT '1',
  `startvel` float NOT NULL,
  `endvel` float NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  KEY `map` (`map`,`style`,`track`,`time`) USING BTREE,
  KEY `auth` (`auth`,`date`,`points`) USING BTREE,
  KEY `time` (`time`) USING BTREE,
  KEY `map2` (`map`) USING BTREE,
  CONSTRAINT `pt_auth` FOREIGN KEY (`auth`) REFERENCES `users` (`auth`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=8158 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;


-- shavit.stagetimes 定义

CREATE TABLE `stagetimes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `style` tinyint NOT NULL DEFAULT '0',
  `track` tinyint NOT NULL DEFAULT '0',
  `stage` tinyint NOT NULL,
  `time` float NOT NULL,
  `auth` int NOT NULL,
  `map` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `points` float NOT NULL DEFAULT '0',
  `jumps` int DEFAULT NULL,
  `date` int DEFAULT NULL,
  `strafes` int DEFAULT NULL,
  `sync` float DEFAULT NULL,
  `perfs` float DEFAULT '0',
  `completions` smallint DEFAULT '1',
  `startvel` float NOT NULL,
  `endvel` float NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  KEY `map` (`map`,`style`,`track`,`stage`,`time`) USING BTREE,
  KEY `auth` (`auth`,`date`,`points`) USING BTREE,
  KEY `time` (`time`) USING BTREE,
  KEY `map2` (`map`) USING BTREE,
  CONSTRAINT `st_auth` FOREIGN KEY (`auth`) REFERENCES `users` (`auth`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=15630 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;