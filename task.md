This is a origin repository I have created for management of matches in Spirit Island. 

Spirit Island is a cooperative board game in which you take role of a spirit in 1-6 players and protect the island from enemies.

So far, I have made a structure, how the code should be organized. The architecture should follow a Clean architecture principals and Domain Driven Design.

Domain
- this should be the main part of the app where the business is defined, using Aggregate roots, entities and value objects.
- it should not have any dependencies on 3rd parties or any other project
- Changes of entities are made to each other by raising domain event and then consuming it

Application
- this should be the place for logic and grouping things together
- it is using MediatR for CQRS, benefiting of various behaviours

Infrastructure
- this should be the place for the configuration of 3rd parties

WebApp
- Blazor server WebApp that is the UI that manages the whole app.

I would like to do the following:

1. Update the repository to the newest standards etc.,
- this is quite old so I want you to update the repository to the newest .NET, use .NET aspire for orchestration, update libraries and overall check the existing code whether it can be improved or updated so we can build on it in the next steps => whole solution review

2. Add the game components
- add the spirits, adversaries, island setups, scenarios, island boards and expansions. All things should be referenced to the given expansion (or starting game) in which it was added so that user can simply check which expansion they own and only the given things will be displayed/available for manage. The best place for the source for these things is the official wiki https://spiritislandwiki.com/index.php?title=Main_Page. For some things I have already created Domains, but i am not sure if this is the best way for implementing it that way.

3. App/business logic
- create the command and queries, update domains so that all logic is implemented and everything is working. main features are:
    - create/manage games
    - user settings (which expansion they own, what match players are there etc.)

4. UI
- all the logic should be implemented in the UI. Also including statistics and graphs. The statistics should be very deep - overall stats, per match player stats, etc.
