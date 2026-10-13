using Godot;
using System;
using System.Diagnostics;

public partial class Fluid2D : Control
{
    int particleSize = 4;
    float restDensity = 0.003f;
    float stiffness = 2;
    float gamma = 7;
	[Export] int ParticleCount = 100;
	Vector2[] Positions;
    //Vector2[] Velocities;
    float[] Densities;
    float[] Pressures;
    float mass = 1.0f;

    //nb: h is usually referred to as smoothing radius
    Slider SmoothingRadius;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Positions = new Vector2[ParticleCount];
        Densities = new float[ParticleCount];
        Pressures = new float[ParticleCount];
        Random r = new Random();

        int x = 0;
        int y = 0;
		for (int i = 0; i < ParticleCount; i++)
        {
			Positions[i] = new Vector2(x * (particleSize * 4) + particleSize / 2, y * (particleSize * 4) + particleSize / 2);
            x++;
            if(x % 40 == 0)
            {
                x = 0;
                y++;
            }
        }

        SmoothingRadius = GetNode<Slider>("SmoothingRadius");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		QueueRedraw();

        CalculateDensity();
        CalculatePressure();
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
