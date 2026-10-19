# Eccomerce Web-API
This is a Web-API project for working with products. The products are universal, meaning the store can be called a marketplace.

## Launching the project
Install VS Studio 2026 and run the server application. It is ready to listen to your HTTP requests. By default, mock data is added to the database; you can delete it using the appropriate requests.
You can find the list of APIs in the file “EcommerceAPI Collection.postman_collection”.
Based on this application, you can develop a console/desktop/browser application.

## Architecture
I chose a layered, traditional Web-API architecture, where the business logic is located in the service layer, and API work is handled in the controller layer.
DTOs are needed for requests and responses and serve as intermediate variants of my Entities.

## What’s new for me
I learned more about Postman, specifically that it can be used to write documentation for your APIs, not just to test endpoints.
Working with more complex tables, especially linked ones, was new for me, and I also mastered pagination for the first time.