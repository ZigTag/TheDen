// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MovementBodyPartComponent : Component
{
    [DataField("walkSpeed"), AutoNetworkedField]
    public float WalkSpeed = MovementSpeedModifierComponent.DefaultBaseWalkSpeed;

    [DataField("sprintSpeed"), AutoNetworkedField]
    public float SprintSpeed = MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

    [DataField("acceleration"), AutoNetworkedField]
    public float Acceleration = MovementSpeedModifierComponent.DefaultAcceleration;
}
