# Space Engineers: Asteroid Ores

A Torch [plugin][plugin] and [mod][mod] for [Space Engineers][game] to better
control ores in asteroids.

Code on [github][github].

[game]: https://www.spaceengineersgame.com/
[plugin]: https://torchapi.com/plugins/view/23f5bd00-e6bd-430c-960e-b66d34c05060
[github]: https://github.com/StalkR/Space-Engineers-Asteroid-Ores
[mod]: https://steamcommunity.com/workshop/filedetails/?id=3037359638

## How it works

Server owner configures which ores spawn by default in space and in certain
regions of space.

The plugin patches the server-side procedural asteroid generation so that only
asteroids with the desired ores spawn.

Because clients are also generating asteroids procedurally, the server tells
them of any asteroid that they should delete, and everything stays in sync.

## Config

Config is loaded and saved as `AsteroidOres.cfg`.

There are 2 main sections:
- default value for space
  - you can define if all ores spawn (`<AllOres>true</AllOres>`) or not
    (`<AllOres>false</AllOres>`)
  - if not, then you list which ores are allowed to spawn
    (`<Ores><string>Stone</string></Ores>`)
  - any asteroid which would spawn with ores not listed in there will not
    spawn
- zones, identified by a sphere at a specified center and radius
  - same as above, you can specific if all ores spawn, and if not, which ores
    can spawn

### Example

Imagine we want the following:
- in space, all ores can spawn except `Platinum`
  - we use `<AllOres>false</AllOres>` then list all ores except `Platinum`
- for 100km around `X:0` `Y:0` `Z:0`, we want to allow all asteroids to spawn,
  so including `Platinum`
  - we define the zone center and radius, and use `<AllOres>true</AllOres>`

The config then looks like this:

```
<?xml version="1.0" encoding="utf-8"?>
<Config xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <AllOres>false</AllOres>
  <Ores>
    <string>Cobalt</string>
    <string>Gold</string>
    <string>Ice</string>
    <string>Iron</string>
    <string>Magnesium</string>
    <string>Nickel</string>
    <string>Stone</string>
    <string>Silicon</string>
    <string>Silver</string>
    <string>Uranium</string>
  </Ores>
  <Zones>
    <Zone>
      <AllOres>true</AllOres>
      <Ores />
      <Center>
        <X>0</X>
        <Y>0</Y>
        <Z>0</Z>
      </Center>
      <Radius>100000</Radius>
    </Zone>
  </Zones>
</Config>
```

## Bugs, comments, questions

Create a [new issue][issue].

[issue]: https://github.com/StalkR/Space-Engineers-Asteroid-Ores/issues/new
