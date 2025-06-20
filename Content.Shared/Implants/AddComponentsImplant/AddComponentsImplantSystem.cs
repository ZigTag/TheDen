// SPDX-FileCopyrightText: 2025 HoboLyra <112722819+hobolyra@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Containers;
using Content.Shared.Implants;

namespace Content.Shared.Implants.AddComponentsImplant;

public sealed class AddComponentsImplantSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddComponentsImplantComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<AddComponentsImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnImplantImplantedEvent(Entity<AddComponentsImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is not {} target)
            return;

        foreach (var component in ent.Comp.ComponentsToAdd)
        {
            // Don't add the component if it already exists
            if (EntityManager.HasComponent(target, _factory.GetComponent(component.Key).GetType()))
                continue;

            EntityManager.AddComponent(target, component.Value);
            ent.Comp.AddedComponents.Add(component.Key, component.Value);
        }
    }

    private void OnRemove(Entity<AddComponentsImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        EntityManager.RemoveComponents(args.Container.Owner, ent.Comp.AddedComponents);

        // Clear the list so the implant can be reused.
        ent.Comp.AddedComponents.Clear();
    }
}
