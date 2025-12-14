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
    parser_id uuid not null,
    url text not null,
    processed boolean not null,
    retry_count integer not null,
    FOREIGN KEY (parser_id) REFERENCES avito_spares_parser.processing_parsers (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.subscriptions
(
    id uuid primary key,
    created timestamptz not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.catalogue_pages
(
    id uuid primary key,
    url text not null,
    processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.catalogue_items
(
    id varchar(64) primary key,
    url text not null,
    photos jsonb not null,
    processed boolean not null,
    retry_count integer not null
);

CREATE TABLE IF NOT EXISTS avito_spares_parser.spares
(
    id varchar(64) primary key,
    url text not null,
    payload jsonb not null,
    processed boolean not null
);