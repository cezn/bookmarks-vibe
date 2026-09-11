create table if not exists outbox
(
  id uuid not null primary key,
  aggregatetype varchar(255) not null,
  aggregateid varchar(255) not null,
  user_id varchar(255) not null,
  type varchar(255) not null,
  payload bytea,
  tracingspancontext text
);
