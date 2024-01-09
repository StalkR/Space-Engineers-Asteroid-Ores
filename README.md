# Space Engineers: Asteroid Ores

A Torch [plugin][plugin] and [mod][mod] for [Space Engineers][game] to better
control ores in asteroids.

Code on [github][github].

[game]: https://www.spaceengineersgame.com/
[plugin]: https://torchapi.com/plugins/view/23f5bd00-e6bd-430c-960e-b66d34c05060
[github]: https://github.com/StalkR/Space-Engineers-Asteroid-Ores
[mod]: https://steamcommunity.com/workshop/filedetails/?id=3037359638

## Installation

1. Install the [plugin] on your Torch server.
2. Add the [mod] to your server, so deleted asteroids are synced to clients.
3. Configure the plugin by creating `AsteroidOres.cfg`, example below.

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
- zones
  - asteroid spherical field: specify center and max radius
  - asteroid hollow spherical field: also specify a min radius
  - asteroid ring: also specify a height, and set the planet's CloudLayer with
    RotationAxis 0/0/0 so it's aligned on the Y plane; gives a washer shape

### Example

Imagine we want the following:
- empty space, no asteroids
  - use: `<AllOres>false</AllOres>` then list no ores
- an asteroid ring with all ores except `Platinum`
  - use: `<Center>`, `<MinRadius>`, `<MaxRadius>` and `<Height>` to define the
    asteroid ring characteristics
  - use: `<AllOres>false</AllOres>` then list all ores except `Platinum`
- an asteroid cluster with all ores
  - use: `<Center>` and `<MaxRadius>` to define the spherical field
  - use: `<AllOres>true</AllOres>` so all ores can spawn


The config then looks like this:

```
<?xml version="1.0" encoding="utf-8"?>
<Config xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <!-- no ores spawn in space, so no asteroids -->
  <AllOres>false</AllOres>
  <Ores />

  <Zones>

    <!-- an asteroid ring around planet X:0 Y:0 Z:0, min 130km, max 80km, height 10km, with all ores except Platinum -->
    <Zone>
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
      <Center>
        <X>0</X>
        <Y>0</Y>
        <Z>0</Z>
      </Center>
      <MaxRadius>130000</MaxRadius>
      <MinRadius>80000</MinRadius>
      <Height>10000</Height>
    </Zone>

    <!-- an asteroid cluster centered on X:1000000 Y:1000000 Z:1000000 of radius 100km, with all ores -->
    <Zone>
      <AllOres>true</AllOres>
      <Ores />
      <Center>
        <X>1000000</X>
        <Y>1000000</Y>
        <Z>1000000</Z>
      </Center>
      <MaxRadius>100000</MaxRadius>
    </Zone>

  </Zones>
</Config>
```

## FAQ

### Asteroid fields feel too empty

Be mindful of how the plugin works: it deletes asteroids not matching the
config rules, so the more ores are restricted, the less asteroids will spawn.

Space Engineers has a tendency to generate asteroids with 2 ores, e.g. Iron and
Platinum, rarely a single ore. So if ores are restricted to just Platinum, such
asteroid with both Iron and Platinum would be deleted, because Iron was not
allowed. Only asteroids with only Platinum will spawn, which is very rare,
therefore the asteroid field will be very empty.

Depending on your Asteroid Ores configuration, you can adjust your server
`<ProceduralDensity>` (decimal value between `0` and `1`) to give you the right
asteroid field density. You can increase it further by modding the vanilla
asteroid generator settings (cf. `AsteroidGenerators.sbc` in the game files).

### I can fly through Asteroids

Make sure you installed the companion [mod] on your server so deleted asteroids
are synced to clients.

Known issue: [spectator mode][issue3] is bugged, if you used it and can fly
through asteroids or don't see asteroids, then reconnect to be sure.

[issue3]: https://github.com/StalkR/Space-Engineers-Asteroid-Ores/issues/3

## Bugs, comments, questions

Create a [new issue][issue].

[issue]: https://github.com/StalkR/Space-Engineers-Asteroid-Ores/issues/new
