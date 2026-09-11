create table push_subscriptions (
  id int primary key generated always as identity,
  user_id varchar(255) not null,
  device_token varchar(2048) not null,
  platform varchar(50) not null,
  endpoint text null,
  auth varchar(255) null,
  p256dh varchar(255) null,
  created_at timestamptz not null,
  updated_at timestamptz not null,
  is_active boolean not null default true
);

create index idx_push_subscriptions_user_id on push_subscriptions(user_id);
create index idx_push_subscriptions_is_active on push_subscriptions(is_active);
create unique index idx_push_subscriptions_unique on push_subscriptions(user_id, device_token, platform) where is_active = true;

create table push_notifications (
  id int primary key generated always as identity,
  user_id varchar(255) not null,
  title varchar(255) not null,
  body text not null,
  data jsonb null,
  created_at timestamptz not null,
  sent boolean not null default false
);

create index idx_push_notifications_user_id on push_notifications(user_id);
create index idx_push_notifications_sent on push_notifications(sent);
create index idx_push_notifications_created_at on push_notifications(created_at);
