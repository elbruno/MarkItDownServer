#!/bin/bash

# Build and Push Docker Image to Registry
# This script builds the Docker image and pushes it to a container registry
# 
# Usage:
#   ./scripts/build-and-push.sh [registry] [image-name] [version]
#
# Examples:
#   ./scripts/build-and-push.sh docker.io myusername/markitdownserver 1.0.0
#   ./scripts/build-and-push.sh ghcr.io elbruno/markitdownserver latest
#   ./scripts/build-and-push.sh myregistry.azurecr.io markitdownserver 1.0.0

set -e

# Configuration
REGISTRY=${1:-"docker.io"}
IMAGE_NAME=${2:-"markitdownserver"}
VERSION=${3:-"latest"}

# Full image name
FULL_IMAGE_NAME="${REGISTRY}/${IMAGE_NAME}:${VERSION}"
FULL_IMAGE_NAME_LATEST="${REGISTRY}/${IMAGE_NAME}:latest"

echo "========================================"
echo "Docker Image Build and Push Script"
echo "========================================"
echo ""
echo "Registry: ${REGISTRY}"
echo "Image Name: ${IMAGE_NAME}"
echo "Version: ${VERSION}"
echo "Full Image: ${FULL_IMAGE_NAME}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

# Build the Docker image
echo "Building Docker image..."
docker build -t ${FULL_IMAGE_NAME} -f dockerfile .

if [ $? -eq 0 ]; then
    echo "✓ Docker image built successfully"
else
    echo "✗ Docker build failed"
    exit 1
fi

# Tag as latest if not already latest
if [ "${VERSION}" != "latest" ]; then
    echo ""
    echo "Tagging image as latest..."
    docker tag ${FULL_IMAGE_NAME} ${FULL_IMAGE_NAME_LATEST}
fi

# Test the image locally
echo ""
echo "Testing the image locally..."
CONTAINER_ID=$(docker run -d -p 8490:8490 ${FULL_IMAGE_NAME})
echo "Container started: ${CONTAINER_ID}"

# Wait for the server to start
echo "Waiting for server to start..."
sleep 5

# Test the health endpoint
if curl -f http://localhost:8490/health > /dev/null 2>&1; then
    echo "✓ Health check passed"
else
    echo "✗ Health check failed"
    docker logs ${CONTAINER_ID}
    docker stop ${CONTAINER_ID}
    docker rm ${CONTAINER_ID}
    exit 1
fi

# Stop and remove test container
docker stop ${CONTAINER_ID}
docker rm ${CONTAINER_ID}

echo ""
echo "========================================"
echo "Image Ready for Push"
echo "========================================"
echo ""
echo "To push to registry, run:"
echo "  docker login ${REGISTRY}"
echo "  docker push ${FULL_IMAGE_NAME}"

if [ "${VERSION}" != "latest" ]; then
    echo "  docker push ${FULL_IMAGE_NAME_LATEST}"
fi

echo ""
echo "Or run this script with auto-push:"
echo "  PUSH=true ./scripts/build-and-push.sh ${REGISTRY} ${IMAGE_NAME} ${VERSION}"
echo ""

# Auto-push if PUSH environment variable is set
if [ "${PUSH}" = "true" ]; then
    echo "Auto-push enabled. Pushing images..."
    echo ""
    
    # Push version tag
    echo "Pushing ${FULL_IMAGE_NAME}..."
    docker push ${FULL_IMAGE_NAME}
    
    if [ $? -eq 0 ]; then
        echo "✓ ${FULL_IMAGE_NAME} pushed successfully"
    else
        echo "✗ Failed to push ${FULL_IMAGE_NAME}"
        exit 1
    fi
    
    # Push latest tag if applicable
    if [ "${VERSION}" != "latest" ]; then
        echo ""
        echo "Pushing ${FULL_IMAGE_NAME_LATEST}..."
        docker push ${FULL_IMAGE_NAME_LATEST}
        
        if [ $? -eq 0 ]; then
            echo "✓ ${FULL_IMAGE_NAME_LATEST} pushed successfully"
        else
            echo "✗ Failed to push ${FULL_IMAGE_NAME_LATEST}"
            exit 1
        fi
    fi
    
    echo ""
    echo "========================================"
    echo "✓ Images pushed successfully!"
    echo "========================================"
    echo ""
    echo "Images available at:"
    echo "  ${FULL_IMAGE_NAME}"
    if [ "${VERSION}" != "latest" ]; then
        echo "  ${FULL_IMAGE_NAME_LATEST}"
    fi
fi

echo ""
echo "Done!"
