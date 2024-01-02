Homework for Artificial Intelligence for Computer Games.

Assignment:
Implement three different steering behaviors, each of which will be placed in a separate scene. For each
behavior, follow the requirements below.
Make the assignment on top of the template from the course vault, while focusing mainly on AI
implementation. The template is a Unity project (version 2020.3.21f1) which contains three scenes, one
for each task. More details about the template are by the end of this document

Task 1: Arrival:
● Template scene: Task1_Arrival
  ○ Contains an AI entity, object to be followed and a simple ground.
● AI moves towards the mouse cursor.
● When the AI gets close enough, it slows down and then stops.


Task 2: Obstacle Avoidance:

● Template scene: Task2_ObstacleAvoidance
  ○ Contains an AI entity, set of spherical obstacles and possible destinations
● AI moves in the scene between randomly selected destinations.
● When AI sees an obstacle, it avoids it.
  ○ When avoiding the obstacle, AI is not allowed to intersect it. However, it is allowed to
  “touch it”.
  ○ The avoidance behavior should feel natural. In other words, the agent should look ahead
  and start avoidance action between crashing into the obstacle. As visualized in the image
  below, the avoidance should resemble case a rather than case b. Sometimes, tweaking
  the algorithm parameters might suffice.


Task 3: Flocking:

● Template scene: Task3_Flocking
  ○ Contains a scene with multiple spawned entities.
● Each AI should know:
  ○ how to separate from entities that are too close.
  ○ how to align with the rest of the group (move in the same direction and velocity).
  ○ how to stay cohesive by moving towards the center of mass of the flock.
