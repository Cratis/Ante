import os,sys
HEADER = """// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""
CELLS = {
 'and_development_is_served_directly': ('Development', False),
 'and_development_is_behind_a_proxy': ('Development', True),
 'and_production_is_served_directly': ('Production', False),
 'and_production_is_behind_a_proxy': ('Production', True),
}
def cells(folder, abstract, cells=CELLS, extra=None):
    for name,(env,proxied) in cells.items():
        body = f'''    protected override string EnvironmentName => "{env}";
    protected override bool Proxied => {'true' if proxied else 'false'};
'''
        if extra:
            body += extra.get(name, '')
        src = HEADER + f'''using Ante.Integration.given;

namespace Ante.Integration.Routes.{folder};

[Collection(ChronicleCollection.Name)]
public class {name} : {abstract}
{{
{body}}}
'''
        os.makedirs(folder, exist_ok=True)
        open(f'{folder}/{name}.cs','w').write(src)
if __name__=='__main__':
    cells(sys.argv[1], sys.argv[2])
