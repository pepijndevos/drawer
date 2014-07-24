CREATE TABLE blogpost (
    id bigserial primary key,
    title text NOT NULL,
    body text NOT NULL,
    date timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

