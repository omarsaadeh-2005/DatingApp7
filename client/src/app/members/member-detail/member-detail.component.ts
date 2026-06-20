import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Member } from 'src/app/_models/member';
import { MembersService } from 'src/app/_services/members.service';
import { GalleryItem, ImageItem } from '@ngx-gallery/core';


@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})
export class MemberDetailComponent implements OnInit {
  member: Member | undefined;
  galleryImages: any[]=[];

  constructor(private memberService: MembersService, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.loadMember();
  }

  getImages(): GalleryItem[] {
  if (!this.member || !this.member.photos) return [];

  return this.member.photos.map(photo => {
    return new ImageItem({
      src: photo.url,
      thumb: photo.url
    });
  });
}

  loadMember(){
    const username=this.route.snapshot.paramMap.get('username');
    if(!username) return;
    this.memberService.getMember(username).subscribe({
      next: member=> {
        this.member=member;
        this.galleryImages = this.getImages();
      }
    })
  }

  
}
