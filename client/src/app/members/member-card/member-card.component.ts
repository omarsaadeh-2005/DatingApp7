import { Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { Member } from 'src/app/_models/member';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-member-card',
  templateUrl: './member-card.component.html',
  styleUrls: ['./member-card.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class MemberCardComponent implements OnInit {
  @Input() member: Member | undefined;
  imageUrl = environment.imageUrl;

  constructor() { }

  ngOnInit(): void {
  }

}
