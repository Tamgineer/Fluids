using Godot;
using System;
using System.Collections.Generic;

public partial class Fluid : Node3D
{
	[Export] CsgBox3D bounding;

	[Export] int ParticleCount;

	[Export] PackedScene ParticleMesh;

	Node3D[] particles;
	Vector3[] Positions;
	Vector3[] Velocities;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		particles = new Node3D[ParticleCount];
		Positions = new Vector3[ParticleCount];
		Velocities = new Vector3[ParticleCount];

		ParticleMesh.Instantiate();
		int x = 0;
		int z = 0;
		for (int i = 0; i < ParticleCount; i++)
		{
			particles[i] = ParticleMesh.Instantiate() as Node3D;
			AddChild(particles[i]);
			Positions[i].X = x; Positions[i].Z = z;
			particles[i].Position = Positions[i];
			Velocities[i].Y = -1;
			x++;
			if(x % 5 == 0)
			{
				x = 0;
				z++;
			}
		}
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		for (int i = 0; i < ParticleCount; i++)
		{
			Velocities[i].Y += -1;
			Positions[i] += Velocities[i] * (float)delta;
			if (Positions[i].Y < -bounding.Size.Y / 2)
			{
				Velocities[i] = -Velocities[i];
				Positions[i].Y = -bounding.Size.Y / 2;
			}
			particles[i].Position = Positions[i];
		}
    }
}
