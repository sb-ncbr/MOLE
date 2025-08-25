What's new in MOLE 2.5
======================

General
-------

- Moved to WebChemistry 2.0 Core => Better PDB parsing and mmCIF support.
- Added the ability to select weight function for computing tunnels (Length + Radius, Length, Constant = each edge has weight 1).
- Better support for custom exits. It is possible to compute tunnels to only 1 user specified exit.
- Added "Free Profile/Radius" for channels. The free radius is the distance to the closest backbone or HET atom.
- Ability to export residues surrounding a cavity (split into internal and surface/boundary part).
- Added PatternQuery support (ignore residues, start points, tunnel filtering) .
- Residue representtion -- added InsertionCode, NAME NUMBER [CHAIN] [i:INSERTIONCODE] [Backbone].
- Improved PyMOL output.

Command line version
--------------------

- New options in <Params> tag:
  * UseCustomExitsOnly="0"/"1"
  * WeightFunction="LengthAndRadius"/"Length"/"Constant"
- New option in <Export> tag:
  * ProfileCSV="0"/"1" - ability to export tunnels in CSV format.
  * JSON="0"/"1" - rich JSON output
- Custom exits tag:
  * Allows the user to specify one or more custom exits as a point or a set of residues (which resolve to a point in their centroid)
  * The exit is selected as the closest boundary tetrahedron within some cavity if it's located within the "OriginRadius"
  
  ```XML  
  <Tunnels>
    ...
    <CustomExits>
      <Exit>
        <Point X="-27.435" Y="0.017" Z="-11.724" />
      </Exit>
      <Exit>
        <Residue Chain="A" SequenceNumber="308" />
        <Residue Chain="A" SequenceNumber="309" />
      </Exit>
    </CustomExits>
    ...
  </Tunnels>
  ```
  
- Charge computation on channel surfaces and centerlines (from potential on grid or atom values).
- Lining residues and physico-chemical properties for cavities.
- Ability to specify weight function for channel computation.
- Free profile of channels (radius is determines by the distance to the closest backbone atom, rather than any atom).
- Custom exits for channels
- PattarnQuery support (specification of channel start/end points, filtering of tunnels, active residues).