import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-roles-modal',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './roles-modal.component.html',
  styleUrl: './roles-modal.component.css'
})
export class RolesModalComponent implements OnInit{
  username='';
  availableRoles: any[]=[];
  selectedRoles: any[]=[];

  constructor(public bsModalRef: BsModalRef) { }

  ngOnInit(): void {
  }

  updateChecked(checkedValue: string){
    const index= this.selectedRoles.indexOf(checkedValue);
    index !== -1? this.selectedRoles.splice(index,1): this.selectedRoles.push(checkedValue);
  }

}
