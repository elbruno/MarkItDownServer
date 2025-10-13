# Use the official Python image from the Docker Hub
FROM python:3.12-slim

# Set the working directory in the container
WORKDIR /app

# Copy the current directory contents into the container at /app
COPY . /app

# Install any needed packages specified in requirements.txt
RUN pip install --no-cache-dir -r ./requirements.txt

# Environment variables for configuration
ENV WORKERS=1
ENV PORT=8490
ENV ENABLE_RATE_LIMIT=false
ENV RATE_LIMIT=60/minute

# Make port 8490 available to the world outside this container
EXPOSE 8490

# Run app.py when the container launches
CMD ["python", "./app.py"]