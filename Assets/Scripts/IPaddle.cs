// Anything the ball can hit as a paddle. The ball reads VelocityY at the moment
// of contact to decide how much spin to put on the shot.
public interface IPaddle
{
    // Vertical speed of the paddle (units per second, + is up)
    float VelocityY { get; }
}
