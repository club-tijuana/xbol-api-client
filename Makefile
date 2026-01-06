#!/bin/bash

.PHONY: help run stop restart build logs health shell

DOCKER_BE = xbol-api-client
UID = $(shell id -u)

# Auto-detect container runtime (podman or docker)
CONTAINER_RUNTIME := $(shell command -v podman 2>/dev/null || command -v docker 2>/dev/null)
ifeq ($(CONTAINER_RUNTIME),)
$(error Neither podman nor docker found in PATH)
endif

# Auto-detect compose command (podman-compose, podman compose, or docker compose)
COMPOSE_CMD := $(shell \
	if command -v podman-compose >/dev/null 2>&1; then \
		echo "podman-compose"; \
	elif podman compose version >/dev/null 2>&1; then \
		echo "podman compose"; \
	elif docker compose version >/dev/null 2>&1; then \
		echo "docker compose"; \
	else \
		echo "docker-compose"; \
	fi)

# Set --format docker for Podman to support HEALTHCHECK
BUILD_FORMAT := $(shell \
	if echo $(COMPOSE_CMD) | grep -q podman; then \
		echo '--podman-build-args "--format docker"'; \
	else \
		echo ""; \
	fi)

help: ## Show this help message
	@echo 'usage: make [target]'
	@echo
	@echo 'targets:'
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' ${MAKEFILE_LIST} | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

run: network ## Start the containers
	U_ID=${UID} ${COMPOSE_CMD} up -d

stop: ## Stop the containers
	U_ID=${UID} ${COMPOSE_CMD} stop

restart: ## Restart the containers
	$(MAKE) stop && $(MAKE) run

build: ## Rebuilds all the containers
	U_ID=${UID} ${COMPOSE_CMD} ${BUILD_FORMAT} build

network: ## Creates the shared network
	@${CONTAINER_RUNTIME} network inspect xbol-network >/dev/null 2>&1 || ${CONTAINER_RUNTIME} network create xbol-network

logs: ## Follow API logs
	${CONTAINER_RUNTIME} logs -f ${DOCKER_BE}

health: ## Check API health endpoint
	@curl -s http://localhost:8080/healthz && echo "" || echo "Health check failed"

shell: ## ssh's into the be container
	U_ID=${UID} ${CONTAINER_RUNTIME} exec -it --user ${UID} ${DOCKER_BE} bash
