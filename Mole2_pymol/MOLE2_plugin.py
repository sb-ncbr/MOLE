#!/usr/bin/env python
# -*- coding: iso-8859-2 -*-
# This is PyMOL plugin for MOLE2.0 - a universal toolkit for rapid location and characterization of channels and pores in biomacromolecules.
#
# If you find this tool usefull for your work, please cite is as follows:
# Sehnal D., Svobodova Varekova R., Berka K., Pravda L., Navratilova V., Banas P., Ionescu C.-M., Otyepka M., Koca J.: MOLE 2.0: Advanced Approach for Analysis of Biomacromolecular Channels, submitted.

from __future__ import division
from __future__ import generators

import os,math
import Tkinter
from Tkinter import *
import Pmw
import distutils.spawn # used for find_executable
from pymol import cmd,selector
import sys
from pymol.cmd import _feedback,fb_module,fb_mask,is_list,_cmd
from pymol.cgo import *
from chempy.models import Indexed
from chempy import Bond, Atom
import subprocess
from xml.dom.minidom import Document,parse
import threading
import platform
import string
import imp
from datetime import datetime
import tkFileDialog
import tkMessageBox
import csv
import pickle


#Globals:
CONFIGFILE = '.MOLE_PluginSettings.txt'
OUTPUT = ''

def __init__(self):
        self.menuBar.addmenuitem('Plugin', 'command',
                                 'Launch MOLE',
                                 label='MOLE 2.0',
                                 command = lambda s=self: MOLE2(s))

class MOLE2:
    def __init__(self,app):
        root = Toplevel(app.root)
        root.title('MOLE 2.0')
        root.resizable(0,0)
        self.parent = root
        #region Create Frame and NoteBook
        self.mainframe = Frame(self.parent, width=463, height=623)
        self.mainframe.pack(fill = 'both', expand = 1)
        self.mainframe.bind('<<WrongExecutable>>',lambda e: self.WhenError(e,'Your MOLE 2.0 executable was not found!'))

        balloon = Pmw.Balloon(self.mainframe)

        self.Points = {}
        self.PARAMS = ['','','']

        self.notebook = Pmw.NoteBook(self.mainframe)
        self.notebook.pack(fill = 'both', expand = 1, padx = 10, pady = 10)
        #endregion

        #region self.mainPage / settings
        self.mainPage = self.notebook.add('Compute Tunnels')
        self.mainPage.focus_set()
        
        w = Pmw.Group(self.mainPage, tag_text='Specify input structure')
        w.pack(fill = 'both')
        
        ww = Pmw.Group(self.mainPage, tag_text='Specify starting point')
        ww.pack(fill = 'both')


        init_structures = ('all', ) + tuple(cmd.get_object_list())

        self.InputStructureBox = Pmw.ScrolledListBox(w.interior(), items=init_structures, labelpos='nw',
                listbox_height = 4, listbox_selectmode=EXTENDED)
        self.InputStructureBox.component('listbox').configure(exportselection=0)
        self.InputStructureBox.pack(fill='both', expand=0, padx=10, pady=5)
        balloon.bind(self.InputStructureBox, 'Select one or more structures in which you want to find channels.')

        self.StartingPointsBox = Pmw.ScrolledListBox(ww.interior(), items=(), labelpos='nw', 
                        listbox_height = 4, listbox_selectmode=EXTENDED)
        self.StartingPointsBox.component('listbox').configure(exportselection=0)
        self.StartingPointsBox.pack(fill='both', expand=0, padx=10, pady=5)
        balloon.bind(self.StartingPointsBox, 'Starting point list. If no starting point is specified, \nMOLE plugin will try to find suitable starting points automatically.\nOtherwise all selected points will be used.')

        self.buttonBox1 = Pmw.ButtonBox(ww.interior())
        self.buttonBox1.pack(fill='both', expand=0, padx=10, pady=5)
        self.buttonBox1.add('AddStartingPoint', text = 'Add Starting Point', 
                      command = lambda : self.AddPoint(self.StartingPointsBox))
        self.buttonBox1.add('RemoveStartingPoint', text = 'Remove Starting Point', 
                      command = lambda : self.RemovePoint(self.StartingPointsBox))
        self.buttonBox1.add('RefreshStructures', text = 'Refresh Structures', 
                      command = lambda : self.SetStructures(self.InputStructureBox))
        self.buttonBox1.alignbuttons()


        g1 = Pmw.Group(self.mainPage, tag_pyclass = None)
        g1.pack(fill = 'both')

        self.OverwriteResults = BooleanVar()
        self.OverwriteResults.set(True)
        self.OverwriteResultsButton = Checkbutton(g1.interior(), text="Overwrite results", variable=self.OverwriteResults,
                    onvalue=True, offvalue=False)
        self.OverwriteResultsButton.grid(column=0, row=0, padx=10, pady=5, sticky=W+E+N+S)
        balloon.bind(self.OverwriteResultsButton, 'If checked MOLE will overwrite old files in output folder. Otherwise, new folder will be created in output folder.')


        self.PoresExport = BooleanVar()
        self.PoresExportButton = Checkbutton(g1.interior(), text="Pores export", variable=self.PoresExport,
                    onvalue=True, offvalue=False)
        self.PoresExportButton.grid(column=1, row=0, padx=10, pady=5, sticky=W+E+N+S)
        balloon.bind(self.PoresExportButton, 'If checked MOLE will automatically search for pores and export results in pores.py and pores.xml files.')

        self.IgnoreHet = BooleanVar()
        self.IgnoreHetButton = Checkbutton(g1.interior(), text="Ignore HETeroatoms", variable=self.IgnoreHet,
                    onvalue=True, offvalue=False)
        self.IgnoreHetButton.grid(column=2, row=0, padx=10, pady=5, sticky=W+E+N+S)
        balloon.bind(self.IgnoreHetButton, 'If checked MOLE will exclude all HETATM entries prior to the calculation.')

        self.SelectWorkingDirectoryButton = Button(g1.interior(), text = 'Save output to:', command=self.SelectWorkingDirectory)
        self.SelectWorkingDirectoryButton.grid(row=1, column=0, sticky=W+E+N+S, padx=10, pady=5)
        self.SelectWorkingDirectoryButton.columnconfigure(0, weight=1)
        balloon.bind(self.SelectWorkingDirectoryButton, 'Where do you wish to save output from MOLE 2.0 plugin.')

        self.WorkingDirectory = Pmw.EntryField(g1.interior(), labelpos = 'w')
        self.WorkingDirectory.grid(row=1, column=1, columnspan=2, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.WorkingDirectory, 'Where do you wish to save output from MOLE 2.0 plugin.')

               
        self.GenerateCSASelectionsButton = Button(g1.interior(), text = 'Generate CSA sites:', command=self.GenerateCSASelections)
        self.GenerateCSASelectionsButton.grid(column=0, row=2, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.GenerateCSASelectionsButton, 'Specify structure by writing its PDB ID and press Generate button.')       
        self.Structure = Pmw.EntryField(g1.interior(), labelpos = 'w')
        self.Structure.grid(column=1, row=2, columnspan=2, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.Structure, 'Specify structure by writing its PDB ID and press Generate button.')       

        self.SelectCSAButton = Button(g1.interior(), text = 'Select CSA.dat file:', command=self.SelectCSA)
        self.SelectCSAButton.grid(column=0, row=3, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.SelectCSAButton, 'Insert location of CSA.dat file containing CSA database for suggesting active sites as a starting points.')

        self.CSA = Pmw.EntryField(g1.interior(), labelpos = 'w')
        self.CSA.grid(column=1, columnspan=2, row=3, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.CSA, 'Insert location of CSA.dat file containing CSA database for suggesting active sites as a starting points.')

        self.SelectExecutableButton = Button(g1.interior(), text = 'MOLE 2.0 location:', command=self.SelectExecutable)
        self.SelectExecutableButton.grid(column=0, row=4, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.SelectExecutableButton, 'Select proper path to the MOLE 2.0 command line location')
        
        self.Executable = Pmw.EntryField(g1.interior(), labelpos = 'w')
        self.Executable.grid(column=1, columnspan=2, row=4, sticky=W+E+N+S, padx=10, pady=5)
        balloon.bind(self.Executable, 'Select proper path to the MOLE 2.0 command line location.')


        self.ComputeTunnelsButton = Button(self.mainPage,text = 'Compute Tunnels', font=("Helvetica", 12, "bold"), command= lambda b='tunnels': self.ConstructParamsAndRun(b))
        self.ComputeTunnelsButton.pack(side=BOTTOM, fill='both', expand=0, padx=10, pady=5)

        #endregion



        #region self.paramsPage / params
        self.paramsPage = self.notebook.add('Settings')

        self.ProbeRadius = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Probe Radius',
                                         entryfield_value = '3', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 1.4, 'max': 45},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.ProbeRadius, 'Radius used for construction of molecular surface.')

        self.InteriorThreshold = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Interior Threshold',
                                         entryfield_value = '1.25', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 0.8, 'max': 45.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.InteriorThreshold, 'Lower bound of the tunnel radius.')

        self.SurfaceCoverRadius = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Surface Cover Radius',
                                         entryfield_value = '10', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 5.0, 'max': 25.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.SurfaceCoverRadius, 'Determines the density of tunnel exits on the molecular surface.')

        self.OriginRadius = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Origin Radius',
                                         entryfield_value = '5', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 0.1, 'max': 10.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.OriginRadius, 'Better starting points are localized within the defined radius from the original starting point.')

        self.BottleneckRadius = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Bottleneck Radius',
                                         entryfield_value = '1.25', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 0.1, 'max': 6.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.BottleneckRadius, 'The minimum radius of a tunnel.')

        self.BottleneckLength = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Bottleneck Length',
                                         entryfield_value = '3', increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 0.0, 'max': 20.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.BottleneckLength, 'Length of a possible profile narrower than the Bottleneck Radius')

        self.CutoffRatio = Pmw.Counter(self.paramsPage, labelpos = 'w', label_text = 'Cutoff Ratio',
                                         entryfield_value = '0.7', increment = 0.05,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.', 'min' : 0.0, 'max': 1.0},
                                         datatype = {'counter' : 'real', 'separator' : '.'})
        balloon.bind(self.CutoffRatio, 'Determines maximum similarity of tunnels centerline. \nIf two tunnels are more similar than the threshold, the longer is discarded.')
        #region load executable location from settingsfile
        
        temp_path = os.path.normcase(str(os.environ['TEMP']) if 'win32' == str.lower(sys.platform) else '/tmp/')
        
        if os.path.exists(CONFIGFILE):
            try:
                f = open(CONFIGFILE, 'r')
                self.PARAMS = list(map(lambda x: os.path.normcase(x),pickle.load(f)))
                self.Executable.setvalue(self.PARAMS[0])
                self.WorkingDirectory.setvalue(self.PARAMS[1])
                self.CSA.setvalue(self.PARAMS[2])
                f.close()
            except:
                self.Executable.setvalue('')
                self.WorkingDirectory.setvalue(temp_path)
                self.CSA.setvalue('')
        else:
            self.WorkingDirectory.setvalue(temp_path)
            self.PARAMS[1] = temp_path
            f = open(CONFIGFILE, 'w')
            pickle.dump(self.PARAMS, f)
            f.close()
        #endregion

        self.params = (self.ProbeRadius, self.InteriorThreshold, self.SurfaceCoverRadius, self.OriginRadius, self.BottleneckRadius, self.BottleneckLength, self.CutoffRatio)
        self.pa = (self.ProbeRadius, self.InteriorThreshold, self.SurfaceCoverRadius, self.OriginRadius, self.BottleneckRadius, self.BottleneckLength, self.CutoffRatio) # self.CSA, self.Executable
        Pmw.alignlabels(self.pa)
        for counter in self.pa:
            counter.pack(fill='both', expand=1, padx=10, pady=5)


        g3 = Pmw.Group(self.paramsPage, tag_text='Additional settings')
        g3.pack(fill = 'both')

        self.ReadAllModels = BooleanVar()
        self.ReadAllModelsButton = Checkbutton(g3.interior(), text="Read all models", variable=self.ReadAllModels,
                    onvalue=True, offvalue=False)
        self.ReadAllModelsButton.pack(side=LEFT)
        balloon.bind(self.ReadAllModelsButton, 'If checked MOLE will use all models of the specified structure for calculation.\nIt is recomended to use it with structure assembly files (*.pdb1, ...).\nThis option is not suitable for NMR structures with more models.')
              
        self.RemoveHydrogens = BooleanVar()
        self.RemoveHydrogensButton = Checkbutton(g3.interior(), text="Remove hydrogens", variable=self.RemoveHydrogens,
                    onvalue=True, offvalue=False)
        self.RemoveHydrogensButton.pack(side=RIGHT)
        balloon.bind(self.RemoveHydrogensButton, 'If checked MOLE will remove all hydrogens from the structure prior to the calculation.')

        #endregion




        #region self.pathPage / settings
        self.pathPage = self.notebook.add('Compute Pores')

        w = Pmw.Group(self.pathPage, tag_text='Specify pores starting points')
        w.pack(fill = 'both')

        self.PathStartingPointsBox = Pmw.ScrolledListBox(w.interior(), items=(), labelpos='nw',
                label_text='Starting Points', listbox_height = 4, listbox_selectmode=EXTENDED)
        self.PathStartingPointsBox.component('listbox').configure(exportselection=0)
        self.PathStartingPointsBox.pack(fill='both', expand=0, padx=10, pady=5)
        balloon.bind(self.PathStartingPointsBox, 'Starting point list. Every starting point must have coresponding end point.')

        self.buttonBox2 = Pmw.ButtonBox(w.interior())
        self.buttonBox2.pack(fill='both', expand=0, padx=10, pady=5)
        self.buttonBox2.add('AddStartingPoint', text = 'Add Starting Points', 
                      command = lambda : self.AddPoint(self.PathStartingPointsBox))
        self.buttonBox2.add('RemoveStartingPoint', text = 'Remove Starting Points', 
                      command = lambda : self.RemovePoint(self.PathStartingPointsBox))
        self.buttonBox2.alignbuttons()

        w = Pmw.Group(self.pathPage, tag_text='Specify pores end points')
        w.pack(fill = 'both')

        self.PathEndPointsBox = Pmw.ScrolledListBox(w.interior(), items=(), labelpos='nw',
                label_text='End Points', listbox_height = 4, listbox_selectmode=EXTENDED)
        self.PathEndPointsBox.component('listbox').configure(exportselection=0)
        self.PathEndPointsBox.pack(fill='both', expand=0, padx=10, pady=5)
        balloon.bind(self.PathEndPointsBox, 'End points list. Every end point must have coresponding starting point.')

        self.buttonBox3 = Pmw.ButtonBox(w.interior())
        self.buttonBox3.pack(fill='both', expand=0, padx=10, pady=5)
        self.buttonBox3.add('AddEndPoint', text = 'Add End Points', 
                      command = lambda : self.AddPoint(self.PathEndPointsBox))
        self.buttonBox3.add('RemoveEndPoint', text = 'Remove end points', 
                      command = lambda : self.RemovePoint(self.PathEndPointsBox))
        self.buttonBox3.alignbuttons()

        self.ComputeTunnelsButton2 = Button(self.pathPage, text = 'Compute pores', font=("Helvetica", 12, "bold"), command= lambda b='pores': self.ConstructParamsAndRun(b))
        self.ComputeTunnelsButton2.pack(fill='both', expand=0, padx=10, pady=5)
        #endregion

        #prev set

        #region READ
        self.readPage = self.notebook.add('Read Channels')
        g2 = Pmw.Group(self.readPage, tag_text='Select a file with previously computed MOLE tunnels/pores')
        g2.pack(fill = 'both')
        self.OpenChannelsButton = Button(g2.interior(), text = 'Open computation results', command=self.OpenChannels, font=("Helvetica", 12, "bold"))
        self.OpenChannelsButton.pack(fill='both', expand=0, padx=10, pady=5)

        #endregion

        #region QuickStartGuide
        self.guidePage = self.notebook.add('Quick Guide')
        Guide = Label(self.guidePage, relief = 'sunken', anchor=W, justify=LEFT, wraplength=400, padx = 10, pady = 10)
        Guide.pack(fill='both')
        Guide.configure(background = 'cornsilk1')
        Guide.configure(text =
                        """Plugin description:\n\nThe plugin is separated into several tabs, the crucial for the calculation are: Compute Tunnels, Settings and Compute Pores. At first, specify location of output folders for the calculation and the location of MOLE 2.0 command line executable. Afterwards, select a structure in the 'input structure listbox' and starting point from the 'starting point listbox'. Additionally if you provide a path to the CSA database[1], the plugin will suggest you the potential starting points.\n\nRun:\n\nAfter selecting one or more structures and one or more starting points by clicking 'Add Starting Point'. Additional search parameters can be adjusted in the Settings tabs. For further info on how to use this please refer the included manual or visit our webpages. Whenever you would feel lost just hover your cursor above any element in order to get tooltip. For more info and news about the MOLE 2.0 visit our webpages. \n\nAlso if you would like to make a suggestion on how to improve our product or send a bug report, contact us via mail: david.sehnal@gmail.com or luky.pravda@gmail.com \n\n\n Happy tunneling\n\n Mole development team. www.mole.chemi.muni.cz\n\n[1] The Catalytic Site Atlas: a resource of catalytic sites and residues identified in enzymes using structural data. Craig T. Porter, Gail J. Bartlett, and Janet M. Thornton (2004) Nucl. Acids. Res. 32: D129-D133.""")
        #endregion

        #region QuickStartGuide
        self.authorsPage = self.notebook.add('Authors')
        Guide = Label(self.authorsPage, relief = 'sunken',anchor=W, justify=LEFT,wraplength=400, padx = 10, pady = 10)
        Guide.pack(fill='both')
        Guide.configure(bg='#52A300', fg='white')
        Guide.configure(text =
                        u"""If you find this tool useful for your work please cite it as:\n\nSehnal et. al. MOLE 2.0. Advanced Approach for Analysis of Biomacromolecular Channels (2013) submitted\n\nIf you were using the web server, which is available at http://mole.upol.cz/ please cite it as:\n\nBerka et. al. MOLEonline 2.0: interactive web-based analysis of biomacromolecular channels. Nucl. Acids Res. (2012) 40(W1): W222-W227 first published online May 2, 2012 doi:10.1093/nar/gks363""")
        #endregion
        Label(self.mainframe, relief = 'sunken',anchor=W, justify=LEFT, bg='#52A300', fg='white', font=("Helvetica", 12),
              padx = 10, pady = 10, text = "(c) 2013 CEITEC & NCBR MU & FCH UPOL\nwww.mole.chemi.muni.cz                                v. 13.7.11").pack(fill='both')

        self.notebook.setnaturalsize()


    def SetStructures(self, listbox):
        listbox.clear()
        structure_selection = ('all',)
        
        for item in cmd.get_object_list():
             structure_selection = structure_selection + (item, )
        
        listbox.setlist(structure_selection)


    def AddPoint(self,listbox):
        dialog = PointDialog(self.mainframe,
                             CSA = self.CSA.get())
        dialog.activate(geometry = 'centerscreenalways')
        if dialog.ReturnValue != None:
            r = dialog.PointName + "  Type: " + dialog.ReturnValue[1]['Element']
            x = listbox.get()
            x += (r,)
            listbox.setlist(x)
            self.Points[dialog.PointName] = dialog.ReturnValue


    def RemovePoint(self,listbox):
        x = ()
        for i in listbox.get():
            if i not in listbox.getvalue():
                x += (i,)
        listbox.setlist(x)

        p = {}
        for i in self.Points.keys():
            if i not in listbox.getvalue() or i[4:5] is '|':
                p[i] = self.Points[i]
        self.Points = p


    def SelectWorkingDirectory(self):
        file_path = tkFileDialog.askdirectory(title='Select Output Directory')

        file_path = os.path.normcase(file_path)
        if file_path != "" and os.access(file_path,os.R_OK and os.W_OK):
            try:
                f = open(os.path.join(file_path,'testfile.txt'),'w')
                f.close()
                f = open(os.path.join(file_path,'testfile.txt'),'r')
                f.close()
            except:
                tkMessageBox.showinfo('Info','Selected output directory does not have sufficient permissions to be used.')
                return
            os.remove(os.path.join(file_path,'testfile.txt'))
            self.WorkingDirectory.setvalue(file_path)

            try:
                self.PARAMS[1] = file_path
                f = open(CONFIGFILE, 'w')
                pickle.dump(self.PARAMS, f)
                f.close()
            except:
                pass
        else:
            tkMessageBox.showinfo('Info','Selected output directory does not have sufficient permissions to be used.')

    def OpenChannels(self):
        file_path = tkFileDialog.askopenfilename(title='Select MOLE tunnel files',
                                             filetypes=[('MOLE tunnels','.xml .pdb .py')])
        file_path = os.path.normcase(file_path)

        extension = os.path.splitext(file_path)[1].lower()
        if len(extension) < 3:
            return

        try:
            if extension == '.py':
                cmd.do("run " + file_path)
            if extension == '.xml':
                self.ParseXMLChannel(file_path)
            if extension == '.pdb':
                self.ParsePDBChannel(file_path)

        except Exception, e:
            print(e)  

    def ParseXMLChannel(self, file_path):
        xml = parse(file_path)
        pool = ['Tunnel', 'Pore']

        for element in pool:
            i = 1
            model = Indexed()
            for tunnel in xml.getElementsByTagName(element):
                for node in tunnel.getElementsByTagName('Node'):
                    self.AppendNode(model, [float(node.getAttribute('X')), float(node.getAttribute('Y')), float(node.getAttribute('Z')), float(node.getAttribute('Radius'))])
                model = self.MakeChannel(model, i)
                i+=1

    def ParsePDBChannel(self, file_path):
        file = open(file_path)
        i = 0
        channelId = 1
        model = Indexed()
        nodes = []
        
        for line in file:
            if line.startswith('HETATM'):
               if channelId != int(line[25:30]):
                   i+=1
                   model = self.MakeChannel(model, i)
                   channelId+=1

               nodes = [float(line[31:39]), float(line[39:47]), float(line[47:55]), float(line[62:68])]
               self.AppendNode(model, nodes)
        file.close()
        i +=1
        self.MakeChannel(model, i)


    def MakeChannel(self, model, i):
        for a in range(len(model.atom)-1):
            bd = Bond()
            bd.index = [a,a+1]
            model.bond.append(bd)
        cmd.load_model(model,"Tunnel" + str(i),state=1)
        cmd.set("sphere_mode","0", "Tunnel" + str(i))
        cmd.set("sphere_color", "red", "Tunnel" + str(i))

        cmd.show("spheres","Tunnel" + str(i))

        return Indexed()

    def AppendNode(self, model ,list):
        at = Atom()
        at.name = '0'
        at.vdw = list[3]
        at.coord = list [:3]
        model.atom.append(at)


    def SelectCSA(self):
        file_path = tkFileDialog.askopenfilename(title='Select CSA.dat file',
                                             filetypes=[('CSA.dat file','.dat')])
        file_path = os.path.normcase(file_path)
        if file_path != "":
            self.CSA.setvalue(file_path)
            try:
                self.PARAMS[2] = file_path
                f = open(CONFIGFILE, 'w')
                pickle.dump(self.PARAMS, f)
                f.close()
            except:
                pass


    def SelectExecutable(self):
        file_path = tkFileDialog.askopenfilename(title='Select MOLE 2.0 Executable',
                                             filetypes=[('MOLE 2.0 Executable','.exe')])
        file_path = os.path.normcase(file_path)
        if file_path != "":
            self.Executable.setvalue(file_path)
            try:
                self.PARAMS[0] = file_path
                f = open(CONFIGFILE, 'w')
                pickle.dump(self.PARAMS, f)
                f.close()      
            except:
                pass

    def ConstructParamsAndRun(self, param):
        self.SetState('disabled')
        parameters = {}
        for i in self.params:
            parameters[i['label_text'].replace(' ', '')] = i.get()

        parameters['BottleneckTolerance'] = parameters.pop('BottleneckLength')
        parameters['MaxTunnelSimilarity'] = parameters.pop('CutoffRatio')

        self.original_view = cmd.get_view()
               
        if not os.path.exists(self.PARAMS[0]):
            self.WhenError('','Your MOLE 2.0 executable was not found!')
            return
        
        if len(cmd.get_object_list()) == 0:
            self.WhenError('','No structures loaded in the PyMOL.')
            return
            
        #region create working directory and test permissions
        if not os.path.exists(self.PARAMS[1]):
            self.WhenError('','No output directory specified.');
            return
        #endregion

        self.wd = os.path.realpath(os.path.normcase(self.PARAMS[1]))

        #region create subdirectory
        if not self.OverwriteResults.get():
            self.wd = os.path.join(self.wd,str(datetime.now().toordinal()))
        if not os.path.exists(self.wd):
            try:
                os.mkdir(self.wd)
            except:
                self.WhenError('','Output directory cannot be created!');
                return

        Structure = 'All'
        structures = []

        doc = Document()
        root = doc.createElement("Tunnels")
        doc.appendChild(root)

        if len(self.InputStructureBox.getvalue()) < 1:
            self.WhenError('','No structure selected');
            return
        elif 'all' in self.InputStructureBox.getvalue():
            for i in cmd.get_object_list():
                structures.append(i)
        else:
            selection = set(self.InputStructureBox.getvalue())
            for s in selection:
                current_structure = cmd.get_object_list(s)
                if current_structure == None:
                    self.WhenError('', 'The structure you have selected is no longer available in the PyMOL object list. Please refresh structures.')
                    return
                else:
                    structures.append(current_structure[0])
            if len(structures) == 1:
                Structure = str(structures[0])

            #region Input
            path = os.path.realpath(os.path.join(self.wd, Structure+'.pdb'))
            e = doc.createElement("Input")

            if self.ReadAllModels.get():
                e.setAttribute('ReadAllModels','1')

            if len(structures) == 1:
                cmd.save(path,structures[0])
            else:
                ex = ''
                for i in structures:
                    ex += i +"|"

                ex = string.rstrip(ex,'|')
                cmd.do('select x,'+ex)
                cmd.save(path,'x')
                cmd.delete('x')
            e.appendChild(doc.createTextNode(path))
            root.appendChild(e)
            #endregion

            #region WorkingDirectory
            e = doc.createElement("WorkingDirectory")
            e.appendChild(doc.createTextNode(self.wd))
            root.appendChild(e)
            #endregion

            #region Params
            e = doc.createElement("Params")
            for i in parameters.keys():
                e.setAttribute(i,parameters[i])
            
            if self.RemoveHydrogens.get():
                e.setAttribute('RemoveHydrogens','1')

            if self.IgnoreHet.get():
                e.setAttribute('IgnoreHETAtoms','1')
            else:
                e.setAttribute('IgnoreHETAtoms','0')
            
            root.appendChild(e)
            #endregion

            #region Export
            e = doc.createElement("Export")
            e.setAttribute('PyMol','1')
            e.setAttribute('PyMolDisplayType', 'Spheres')

            if param == 'pores':
                e.setAttribute('Tunnels', '0')

            if self.PoresExport.get():
                e.setAttribute('Pores','1')           

            root.appendChild(e)
        e = doc.createElement("Origin")

        if param == 'tunnels':
            self.ComputeTunnelsButton.config(text='Processing... Please wait.')
            if len(self.StartingPointsBox.get()) == 0:
                e.setAttribute('Auto','1')
            else:
                e.setAttribute('Auto','0')
        
            if len(self.StartingPointsBox.getvalue()) == 1:
                for i in self.Points.keys():
                    if i in self.StartingPointsBox.getvalue()[0]:
                        self.AppendOrigin(doc,e,self.Points[i])

            if len(self.StartingPointsBox.getvalue()) > 1:
                for i in self.Points.keys():
                    for j in self.StartingPointsBox.getvalue():
                        if i in j:
                            f = doc.createElement('Pinned')
                            self.AppendOrigin(doc,f,self.Points[i])
                            e.appendChild(f)
        #endregion

        #region PathPointsBoxes
        if param == 'pores':
            if len(self.PathStartingPointsBox.getvalue()) > 0 and len(self.PathEndPointsBox.getvalue()) > 0:
                e.setAttribute('Auto','0')
            else:
                self.WhenError('','No Pore points selected. Please select at least one start pair of points.')
                return

            self.ComputeTunnelsButton2.config(text='Processing... Please wait.')
            start_values = self.PathStartingPointsBox.getvalue()
            stop_values = self.PathEndPointsBox.getvalue()

            for i in start_values: # todo here
                for j in stop_values:
                    start = self.GetElement(i)
                    stop = self.GetElement(j)

                    if start is not '42' and stop is not '42':
                        f = doc.createElement('Path')
                        g = doc.createElement('Start')
                        self.AppendOrigin(doc,g,start)
                        f.appendChild(g)
                        g = doc.createElement('End')
                        self.AppendOrigin(doc,g,stop)
                        f.appendChild(g)
                        e.appendChild(f)

        root.appendChild(e)
        #endregion

        f = open(os.path.join(self.wd, 'params.xml'), 'w')
        doc.writexml(f)
        f.close()

        self.parent.update()
        self.SetState('disabled')
        plat = str.lower(sys.platform)
        
        if 'win32' not in plat:
            if self.MonoTest() is not 1:
                self.WhenError('Error','Mono environment required for MOLE 2.0 in non-windows environment is not installed. Please go to www.mono-project.com and install it.')
                self.RenameButtons(param)
                return

        try:
            t = threading.Thread(target = self.RunSubProcess)
            t.daemon = True
            t.start()
            t.join()
        except Exception, e:
            self.WhenError('','An error occurred during processing. If this problem persists and you are a non-windows user try installing \'mono-devel\' package. If it does not help, please send the text below to the authors with the description \n#################\n ' + str(e) )
            self.RenameButtons(param)
            return

        self.RenameButtons(param)
        self.WhenComputationDone(OUTPUT)

    def GetElement(self, selection):
        for key in self.Points.keys():
            if key in selection:
                return self.Points[key]
        return '42'
#        self.WhenError('','Selection ' + key + ' was not found')

    def RenameButtons(self, param):
        if param is 'tunnels':
            self.ComputeTunnelsButton.config(text='Compute Tunnels')
        else:
            self.ComputeTunnelsButton2.config(text='Compute Pores')

    def SetState(self,state):
        self.GenerateCSASelectionsButton.config(state = state)
        self.Structure.component('entry').config(state = state)
        self.PoresExportButton.config(state = state)
        self.IgnoreHetButton.config(state = state)
        self.OverwriteResultsButton.config(state = state)
        self.ComputeTunnelsButton.config(state = state)
        self.ComputeTunnelsButton2.config(state = state)
        self.SelectExecutableButton.config(state = state)
        self.SelectWorkingDirectoryButton.config(state = state)
        self.SelectCSAButton.config(state = state)
        self.CSA.component('entry').config(state = state)
        self.Executable.component('entry').config(state = state)
        self.WorkingDirectory.component('entry').config(state = state)

        for w in self.params:
            w.component('entry').config(state = state)
        self.buttonBox1.button(0).config(state = state)
        self.buttonBox1.button(1).config(state = state)
        self.buttonBox1.button(2).config(state = state)
        self.buttonBox2.button(0).config(state = state)
        self.buttonBox2.button(1).config(state = state)
        self.buttonBox3.button(0).config(state = state)
        self.buttonBox3.button(1).config(state = state)

    def WhenError(self,event,message):
        self.SetState('normal')
        tkMessageBox.showinfo('Error', message)

    def AppendOrigin(self,doc, element, point):
        for j in range(1,len(point)):
            e = doc.createElement(point[j]['Element'])
            for i in point[j].keys():
                if i != 'Element':
                    e.setAttribute(i,point[j][i])
            element.appendChild(e)

    def RunSubProcess(self):
        if os.path.exists(os.path.realpath(self.PARAMS[0])):
            plat = str.lower(sys.platform)
            args = [ os.path.realpath(self.PARAMS[0]), os.path.join(self.wd, 'params.xml')]
            output = ''
            if 'win32' in plat:
                output = subprocess.Popen(args, stdout=subprocess.PIPE).communicate()[0].split('\n')
            else:
                output = subprocess.Popen(['mono']+args, stdout=subprocess.PIPE).communicate()[0].split('\n')     

            output = [e for e in output if e.startswith("Found")][0]
            if '0 tunnels' in output:
                output += '\n\nTry adjusting settings in the \'Settings\' tab.'
        else:
            output = 'Your MOLE 2.0 executable was not found!'
        global OUTPUT
        OUTPUT = output



    def MonoTest(self):
        try:
            subprocess.Popen(['mono', '-V'])
            return 1
        except:
            return 0


    def WhenComputationDone(self, out):
        tkMessageBox.showinfo('Computation is done', out)
        self.SetState('normal')
        tunnelsScript = os.path.join(self.wd,'tunnels.py')
        pathsScript = os.path.join(self.wd,'paths.py')
        poresScript = os.path.join(self.wd,'pores.py')
        autoporesScript = os.path.join(self.wd,'autopores.py')
        dom = parse(os.path.join(self.wd,'tunnels.xml'))
        for e in dom.getElementsByTagName('Exception'):
            tkMessageBox.showinfo('Exception',e.getAttribute('Text'))
        if os.path.exists(tunnelsScript):
            previous = cmd.get_object_list()
            imp.load_source('MOLE_Tunnels'+str(datetime.now().toordinal()),tunnelsScript)
            for o in cmd.get_object_list():
                if o not in previous:
                    cmd.group('Tunnels',o)
        if os.path.exists(pathsScript):
            previous = cmd.get_object_list()
            imp.load_source('MOLE_Paths'+str(datetime.now().toordinal()),pathsScript)
            for o in cmd.get_object_list():
                if o not in previous:
                    cmd.group('Pores',o)
        if os.path.exists(poresScript) & self.PoresExport.get():
            previous = cmd.get_object_list()
            imp.load_source('MOLE_Pores'+str(datetime.now().toordinal()),poresScript)
            for o in cmd.get_object_list():
                if o not in previous:
                    cmd.group('Pores',o)
        if os.path.exists(autoporesScript)  & self.PoresExport.get():
            previous = cmd.get_object_list()
            imp.load_source('MOLE_AutoPores'+str(datetime.now().toordinal()),autoporesScript)
            for o in cmd.get_object_list():
                if o not in previous:
                    cmd.group('AutoPores',o)
        self.SetState('normal')
        cmd.set_view(self.original_view)
        

    def GenerateCSASelections(self):
        active_sites = ()
        csa_structure = self.Structure.get().lower()
    
        if self.PARAMS[2] == '':
            self.WhenError('','Specify CSA.dat file for search of active sites.')
            return
        if csa_structure == '':
                if cmd.get_object_list():
                    self.WhenError('','No structure was found in PyMOL object list.')
                    return
                self.WhenError('','No structure was defined.')
                return
        else:
            if csa_structure not in map(lambda x:x.lower(),cmd.get_object_list()):
                self.WhenError('',csa_structure + ' was not found in PyMOL object list.')
                return
        try:        
            file = csv.reader(open(self.PARAMS[2],'r'))
            sites=0
            s = ''
            listbox_entry = ()
            points_entry =()
            points_entry += ({'Structure': '', 'Type': 'CSA'},)

            data = []
            
            for row in file:
                if row[0] == csa_structure:
                    data.append(row)
                    sites = long(row[1])
                    
            for i in range(sites + 1):
                for row in data:
                    if(row[1] == str(i)):
                        s += ' (chain ' + row[3] + ' & resi ' + row[4] + ') |' #(row[3] + '/' + row[2] + ' ' + row[4] + '/',)
                        points_entry += ({'Element': 'Residue',
                                            'Name': row[2],
                                            'Chain': row[3],
                                            'SequenceNumber':row[4]},)
                        listbox_entry +=(row[3]+row[4],)
                if(len(s)>1):
                    y = ''
                    for j in range(len(listbox_entry)-1):
                        y += listbox_entry[j] + ','
                    y += listbox_entry[len(listbox_entry)-1] + ','
                    x = csa_structure + ' & (' + s[0:-1] + ')'
                    cmd.select(csa_structure+ '_' + y,x)
                    self.Points[csa_structure+ '|' + y[0:-1]] = points_entry
                    active_sites += (y, )
                    s = ''
                    listbox_entry = ()
                    points_entry =()
                    points_entry += ({'Structure': '', 'Type': 'CSA'},)
            
            self.FillBoxes(tuple(map(lambda x: csa_structure + '|' + x[0:-1],active_sites),))
            self.Structure.clear()
            
            if(len(active_sites) < 1):
                self.WhenError('','No active sites found for the \'' + csa_structure + '\'')
            
        except:
            self.WhenError('','An error occured during the proccessing of CSA.dat file. Did you provide it in a correct format?')

    def FillBoxes(self, content):
        content1 = tuple(filter(lambda x: x not in self.StartingPointsBox.get(), content))
        content2 = tuple(filter(lambda x: x not in self.PathStartingPointsBox.get(), content))
        content3 = tuple(filter(lambda x: x not in self.PathEndPointsBox.get(), content))
        
        self.StartingPointsBox.setlist(tuple(self.StartingPointsBox.get()) + content1)
        self.PathStartingPointsBox.setlist(tuple(self.PathStartingPointsBox.get()) + content2)
        self.PathEndPointsBox.setlist(tuple(self.PathEndPointsBox.get()) + content3)

            

class PointDialog(Pmw.Dialog):
    """Creates point dialog for adding starting or end point"""
    def __init__(self, parent = None,**kw):
        # Initialise base class (after defining options).
        Pmw.Dialog.__init__(self, parent,command=self.Return)
        self.CSA = ''
        if kw.has_key('CSA'):
            self.CSA = kw['CSA']
        self.PointName = 'Point007'
        if kw.has_key('PointName'):
            self.PointName = kw['PointName']
        self.Structure = ''
        if kw.has_key('Structure'):
            self.Structure = kw['Structure']
        # Create the components.
        interior = self.interior()

        balloon = Pmw.Balloon(interior)

        self.PointGroup = []
        self.PointVar = IntVar()
        self.PointVar.set(0)

        #region point selection
        w = Pmw.Group(interior, tag_pyclass = Radiobutton, tag_text='PyMOL selection (eg.: \"(sele)\")',
                      tag_value = 0, tag_variable = self.PointVar)
        w.pack(fill = 'both', expand = 1)

        self.Point0 = Pmw.EntryField(w.interior(), labelpos = 'w', label_text = 'Point:', value = '(sele)')
        self.Point0.pack(fill='both', expand=1, padx=10, pady=5)
        balloon.bind(self.Point0, 'Example: (sele)\nThe point is defined as a center of mass of selected residues. \n'
                            +'You can choose residues using by selecting them in PyMOL and writing selection name. \n')

        self.PointGroup.append(w)
        #endregion

        #region starting point coords
        w = Pmw.Group(interior, tag_pyclass = Radiobutton, tag_text='Point coordinates',
                      tag_value = 2, tag_variable = self.PointVar)
        w.pack(fill = 'both', expand = 1)

        self.PointX = Pmw.Counter(w.interior(), labelpos = 'w', label_text = 'X',
                                         entryfield_value = 0, increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.'},
                                         datatype = {'counter' : 'real', 'separator' : '.'},
                                         entryfield_command = self.ShowCrisscross)
        self.PointX.component('uparrow').bind("<Button 1>",self.WhenArrowClicked, add="+")
        self.PointX.component('downarrow').bind("<Button 1>",self.WhenArrowClicked, add="+")

        self.PointY = Pmw.Counter(w.interior(), labelpos = 'w', label_text = 'Y',
                                         entryfield_value = 0, increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.'},
                                         datatype = {'counter' : 'real', 'separator' : '.'},
                                         entryfield_command = self.ShowCrisscross)
        self.PointY.component('uparrow').bind("<Button 1>",self.WhenArrowClicked, add="+")
        self.PointY.component('downarrow').bind("<Button 1>",self.WhenArrowClicked, add="+")

        self.PointZ = Pmw.Counter(w.interior(), labelpos = 'w', label_text = 'Z',
                                         entryfield_value = 0, increment = 0.1,
                                         entryfield_validate = {'validator' : 'real', 'separator' : '.'},
                                         datatype = {'counter' : 'real', 'separator' : '.'},
                                         entryfield_command = self.ShowCrisscross)
        self.PointZ.component('uparrow').bind("<Button 1>",self.WhenArrowClicked, add="+")
        self.PointZ.component('downarrow').bind("<Button 1>",self.WhenArrowClicked, add="+")

        self.ComputeCenterButton = Button(interior,text = 'Compute Center',
                                          command=self.ComputeCenter)
        
        c = (self.PointX, self.PointY, self.PointZ,self.ComputeCenterButton)
        Pmw.alignlabels(c)
        for i in c:
            i.pack(fill='both', expand=1, padx=10, pady=5)

        self.PointGroup.append(w)
        #endregion

        Pmw.aligngrouptags(self.PointGroup)

        # Check keywords and initialise options.
        self.initialiseoptions()

    def ComputeCenter(self):
        sel=cmd.get_model('(all)')
        centx=0
        centy=0
        centz=0
        cnt=len(sel.atom)
        if (cnt==0):
           return (0,0,0)
        for a in sel.atom:
            centx+=a.coord[0]
            centy+=a.coord[1]
            centz+=a.coord[2]
        centx/=cnt
        centy/=cnt
        centz/=cnt
        self.PointX.component('entryfield').setentry(centx)
        self.PointY.component('entryfield').setentry(centy)
        self.PointZ.component('entryfield').setentry(centz)
        self.ShowCrisscross()

    def ShowCrisscross(self):
        obj = [
        LINEWIDTH, 3,

        BEGIN, LINE_STRIP,
        VERTEX, float(float(self.PointX.get())-0.5), float(self.PointY.get()), float(self.PointZ.get()),
        VERTEX, float(float(self.PointX.get())+0.5), float(self.PointY.get()), float(self.PointZ.get()),
        END,

        BEGIN, LINE_STRIP,
        VERTEX, float(self.PointX.get()), float(float(self.PointY.get())-0.5), float(self.PointZ.get()),
        VERTEX, float(self.PointX.get()), float(float(self.PointY.get())+0.5), float(self.PointZ.get()),
        END,

        BEGIN, LINE_STRIP,
        VERTEX, float(self.PointX.get()), float(self.PointY.get()), float(float(self.PointZ.get())-0.5),
        VERTEX, float(self.PointX.get()), float(self.PointY.get()), float(float(self.PointZ.get())+0.5),
        END

        ]
        cmd.delete(self.PointName)
        view = cmd.get_view()
        cmd.load_cgo(obj,self.PointName)
        cmd.set_view(view)

    def WhenArrowClicked(self,event):
        self.ShowCrisscross()

    def Return(self,result):
        self.ReturnValue = None
        if result == 'OK':
            if self.PointVar.get() == 0:
                if self.Point0.get() == '':
                    self.WhenError('','Such selection does not exist in PyMOL.')
                    return
                try:
                    model = cmd.get_model(self.Point0.get())
                except:
                    self.WhenError('','Point Selection is not defined!')
                    return
            self.ReturnValue = self.ComputeReturnValue(self.PointVar.get())

            if self.ReturnValue[1]['Element'] == 'Residue':
                self.PointName = self.Point0.get()
            else:
                self.PointName = '['+str(round(float(self.ReturnValue[1]['X']),3)) + ', ' + str(round(float(self.ReturnValue[1]['Y']),3)) + ', ' + str(round(float(self.ReturnValue[1]['Z']),3)) + ']'
        self.deactivate()

    def WhenError(self,event,message):
        tkMessageBox.showinfo('Error', message)

    def ComputeReturnValue(self,type):
        value = ()
        if type == 0:
            value += ({'Structure': self.Structure, 'Type': 'Selection_'+self.Point0.get()},)
            try:
                model = cmd.get_model(self.Point0.get())
                help = []
                for atom in model.atom:
                    help += [[atom.chain, atom.resn, str(atom.resi)]]
                origin = self.Distinct(help)
                for a in origin:
                    value += ({'Element': 'Residue',
                              'Name': a[1],
                              'Chain': a[0],
                              'SequenceNumber': a[2]},)
            except:
                self.WhenError('','Starting Point Selection is not defined!');
                return
        if type == 2:
            value += ({'Structure': self.Structure, 'Type': 'Point'},)
            value += ({'Element': 'Point', 
                      'X': self.PointX.get(), 
                      'Y': self.PointY.get(), 
                      'Z': self.PointZ.get()},)
        return value

    def Distinct(self,l):
        distinct = []
        for i in l:
            if i not in distinct:
                distinct += [i]
        return distinct
