CREATE SCHEMA IF NOT EXISTS avito_spares_parser;

CREATE TABLE IF NOT EXISTS avito_spares_parser.stages
(
    id uuid primary key,
    name varchar(128) not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.processing_parsers
(
    id uuid primary key,
    domain varchar(128) not null,
    type varchar(128) not null,
    finished timestamptz,
    entered timestamptz not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.processing_parser_links
(
    id uuid primary key,    
    url text not null,
    processed boolean not null,
    retry_count integer not null    
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.catalogue_pages
(
    id uuid primary key,
    url text not null,
    processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.spares
(
  id varchar(128) primary key,
  url text not null,
  price bigint not null, 
  is_nds boolean not null,
  address text not null,
  photos jsonb not null,
  oem text not null,
  processed boolean not null,
  retry_count integer not null,
  type varchar(128),
  title varchar(256)
);