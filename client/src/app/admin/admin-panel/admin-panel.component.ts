import { Component } from '@angular/core';
import { TabsetComponent, TabDirective } from "ngx-bootstrap/tabs";
import { HasRoleDirective } from "src/app/_directives/has-role.directive";
import { AppComponent } from 'src/app/app.component';
import { UserManagementComponent } from '../user-management/user-management.component';
import { PhotoEditorComponent } from 'src/app/members/photo-editor/photo-editor.component';
import { PhotoManagementComponent } from '../photo-management/photo-management.component';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [TabsetComponent, TabDirective, HasRoleDirective, UserManagementComponent,PhotoManagementComponent],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.css',
})
export class AdminPanelComponent {

}
