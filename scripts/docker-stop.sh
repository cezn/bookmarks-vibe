#!/bin/bash

docker-compose -f ./compose.yml --profile db --profile monitoring stop
