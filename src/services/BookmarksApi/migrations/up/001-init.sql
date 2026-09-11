create table bookmarks (
  id int primary key generated always as identity,
  title varchar(512) not null,
  url varchar(2048) not null,
  summary text null,
  created_at timestamptz not null,
  update_at timestamptz not null,
  tags text[] not null
);

create table tags (
  id int primary key generated always as identity,
  name varchar(255) not null,
  usage_count int not null default 0
);

create unique index idx_tags_name on tags(name);

