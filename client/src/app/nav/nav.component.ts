import { Component, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  imageUrl = environment.imageUrl;
  model: any ={};

  constructor(public accountService: AccountService, private router: Router,
    private toastr:ToastrService) { }

  ngOnInit(): void {
  }
  
  login(){
    this.accountService.login(this.model).subscribe({
      next: _ => this.router.navigateByUrl('/members'),
      })
  }

  logout(){
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
