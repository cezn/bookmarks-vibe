#!/bin/bash

# Script to forward Docker container ports from localhost to hostr remote host
# Discovers container IPs and sets up SSH tunnels

REMOTE_HOST="hostr"
SERVICES=("redis" "mailhog" "db" "aspire-dashboard" "console" "kafka-connect")

# Port mappings: service -> (container_port, local_port)
declare -A PORTS
PORTS[redis]="6379 8001"
PORTS[mailhog]="8025"
PORTS[db]="5432"
PORTS[console]="8084:8080"
PORTS[aspire-dashboard]="18888"
PORTS[kafka-connect]="8083"

echo "Discovering container IPs on $REMOTE_HOST..."

# Get container IPs
declare -A CONTAINER_IPS
for service in "${SERVICES[@]}"; do
  echo -n "Looking up $service... "

  # SSH to remote host and get container IP
  container_ip=$(ssh "$REMOTE_HOST" \
    "docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' \$(docker ps -q -f name=$service)" 2>/dev/null)

  if [ -z "$container_ip" ]; then
    echo "NOT FOUND (container may not be running)"
  else
    echo "IP: $container_ip"
    CONTAINER_IPS[$service]=$container_ip
  fi
done

echo ""
echo "Building SSH port forwarding command..."

# Build SSH tunnel command
SSH_CMD="ssh"
for service in "${SERVICES[@]}"; do
  if [ -n "${CONTAINER_IPS[$service]}" ]; then
    ip="${CONTAINER_IPS[$service]}"
    port_pairs="${PORTS[$service]}"

    for port_mapping in $port_pairs; do
      if [[ $port_mapping == *:* ]]; then
        src_port=${port_mapping%%:*}
        dst_port=${port_mapping##*:}
      else
        src_port=$port_mapping
        dst_port=$port_mapping
      fi
      SSH_CMD="$SSH_CMD -L $src_port:$ip:$dst_port"
    done
  fi
done

SSH_CMD="$SSH_CMD -N $REMOTE_HOST"

echo "Command to execute:"
echo ""
echo "$SSH_CMD"
echo ""
echo "Press Enter to execute, or Ctrl+C to cancel..."
read

echo "Starting port forwarding..."
echo "Press Ctrl+C to stop"
echo ""

eval "$SSH_CMD"
