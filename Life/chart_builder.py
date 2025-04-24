import matplotlib.pyplot as plot 
import os
density = []
generation = []

script_dir = os.path.dirname(os.path.abspath(__file__))
data_path = os.path.join(script_dir, 'data.txt')
output_path = os.path.join(script_dir, 'plot.png')

with open(data_path, 'r') as file:
    next(file)
    for line in file:
        values = line.strip().split()
        density.append(float(values[0].replace(',', '.')))  
        generation.append(int(values[1]))  


plot.figure(figsize=(10, 6))
plot.plot(density, generation , marker='o', linestyle='-', color='r')
plot.xlabel('Filing dencity')
plot.ylabel('Generation number')
plot.title('Dependence of stable generation on filling density')
plot.grid(True) 
plot.savefig(output_path, dpi=300, bbox_inches='tight')
plot.show()