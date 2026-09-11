Outbox implementation.

Currently, each message in a topic has its own schema.

It is also possible to use 'oneof' union resulting in a single schema for every message in a topic. See https://github.com/confluentinc/tutorials/tree/master/multiple-event-types-protobuf/kafka/src/main.
