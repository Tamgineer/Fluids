using Godot;
using System;
using System.Diagnostics;

public partial class Fluid2D : Control
{
    int particleSize = 4;
    float restDensity = 0.003f;
    float stiffness = 2;
    float gamma = 7;
    float g = 1;
	[Export] int ParticleCount = 100;
	Vector2[] Positions;
    Vector2[] Velocities;
    Vector2[] Accelerations;
    float[] Densities;
    float[] Pressures;
    float mass = 1.0f;

    //nb: h is usually referred to as smoothing radius
    Slider SmoothingRadius;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Positions = new Vector2[ParticleCount];
        Velocities = new Vector2[ParticleCount];
        Accelerations = new Vector2[ParticleCount];

        Densities = new float[ParticleCount];
        Pressures = new float[ParticleCount];
        Random r = new Random();

        int x = 0;
        int y = 0;

		for (int i = 0; i < ParticleCount; i++)
        {
			Positions[i] = new Vector2(x * (particleSize * 2) + particleSize, y * (particleSize * 2) + particleSize);
            x++;
            if((x * (particleSize * 2) + particleSize) >= Size.X)
            {
                x = 0;
                y++;
            }
        }

        SmoothingRadius = GetNode<Slider>("SmoothingRadius");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		QueueRedraw();
        CalculateDensity();
        CalculatePressure();
        for (int i = 0; i < ParticleCount; i++)
        {
            Accelerations[i] = Vector2.Zero;
            for (int j = 0; j < ParticleCount; j++)
            {
                if (i == j) continue;
                float dst = Positions[i].DistanceTo(Positions[j]);
                //think this is supposed to be normalised
                Vector2 dir = (Positions[i] - Positions[j]).Normalized();
                float slope = -WeightedDensityDerivative(dst, (float)SmoothingRadius.Value);
                Accelerations[i] += dir * slope;
            }
        }

        for(int i = 0; i < ParticleCount; i++)
        {
            Velocities[i] += Accelerations[i] * (float)delta;
            Positions[i] += Velocities[i] * (float)delta;

            if(Positions[i].X <= 0)
            {
                Positions[i].X = 0;
                Velocities[i].X *= -1;
            }

            if (Positions[i].Y <= 0)
            {
                Positions[i].Y = 0;
                Velocities[i].Y *= -1;
            }

            if (Positions[i].X >= Size.X)
            {
                Positions[i].X = Size.X;
                Velocities[i].X *= -1;
            }

            if (Positions[i].Y >= Size.Y)
            {
                Positions[i].Y = Size.Y;
                Velocities[i].Y *= -1;
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();
		DrawCircle(GetLocalMousePosition(), (float)SmoothingRadius.Value, Color.FromHsv(0, 0, 0.5f));
        for(int i = 0; i < ParticleCount; i++)
        {
            DrawCircle(Positions[i], particleSize, Color.FromHsv(280/360, 1, (Densities[i]/(float)SmoothingRadius.Value) * 1000));
        }

    }

    //NOTE: For 3D we need to use a different weight kernel
    //r is usually denoted as distance
    float WeightedDensityKernel(float distance)
    {
        float h2 = (float)(SmoothingRadius.Value * SmoothingRadius.Value);
        float r2 = distance * distance;
        if (distance >= 0 && distance < SmoothingRadius.Value)
        {
            return (4 / (MathF.PI * MathF.Pow((float)SmoothingRadius.Value, 8))) * Mathf.Pow(h2 - r2, 3);
        }
        else
        {
            return 0;
        }
    }

    //shamefully copied off Sebastian Lague
    //https://www.youtube.com/watch?v=rSKMYc1CQHE&t=622s
    //We still need to calculate the derivative when we do the 3d version
    static float WeightedDensityDerivative(float dst, float radius)
    {
        if (dst > radius) return 0;
        float f = dst * dst - radius * radius;
        float scale = -24 / MathF.PI * MathF.Pow(radius, 8);
        return scale * dst * f * f;
    }

    void CalculateDensity()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            Densities[i] = 0;
            for (int j = 0; j < ParticleCount; j++)
            {
                if (i == j) { continue; }
                Densities[i] += mass * WeightedDensityKernel(Positions[i].DistanceTo(Positions[j]));
            }
        }
    }

    void CalculatePressure()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            var p = (stiffness * restDensity / gamma) * (MathF.Pow(Densities[i] / restDensity, gamma) - 1);
            Pressures[i] = p > 0 ? p : 0;
        }
    }
}
