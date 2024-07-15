# Overview

This particular project is meant to create a foundation for all of the logic and rules for the Amia persistent world.
It will eventually surpass the old AmiaReforged.Core project, but in the meantime provides a different approach to the same problem.

# Understanding this Project's Structure

This csproject is organized into Features, Database, and Tests. Each of these folders contains a number of projects that are meant to be used together.

## Systems

This is where the core logic of each system is stored. Each feature is meant to be more of a vertical
slice in the application. In essence, each feature is a bounded context. Each feature is meant to be as self-contained as possible.

## Database

This is where the integration logic with the database is stored. This is where the database context is defined, and where the
EFCore models are made. This is also where the migrations are stored. This is kept separate so that the concept of persisting data and modeling
data are kept separate from the core logic of the application.

# Caveat Emptor

This is entirely experimental. It could be likely that this project will eventually be abandoned, with its features
returned to the Core project. However, it is also possible that this project will be the foundation for the future of Amia.